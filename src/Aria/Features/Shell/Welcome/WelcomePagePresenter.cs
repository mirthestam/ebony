using Aria.Core;
using Aria.Core.Connection;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell.Welcome;

public sealed record ConnectDialogResult(ConnectDialogOutcome Outcome, IConnectionProfile? Profile);

public partial class WelcomePagePresenter(
    IConnectionProfileProvider connectionProfileProvider,
    IConnectDialogPresenter connectDialogPresenter,
    IConnectionProfileFactory connectionProfileFactory,
    IAriaControl ariaControl,
    IMessenger messenger,
    ILogger<WelcomePagePresenter> logger) : IRootPresenter<WelcomePage>
{
    private CancellationTokenSource? _refreshCancellationTokenSource;
    private CancellationTokenSource? _discoveryCancellationTokenSource;

    public WelcomePage? View { get; private set; }

    public void Attach(WelcomePage view, AttachContext context)
    {
        connectionProfileProvider.DiscoveryCompleted += ConnectionProfileProviderOnDiscoveryCompleted;

        View = view;

        View.ConnectAction.OnActivate += ConnectActionHandler;
        View.NewAction.OnActivate += NewConnectionHandler;
        View.ConfigureAction.OnActivate += ConfigureConnectionHandler;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Start disk refresh (cancellable via refresh CTS)
        await RefreshConnectionsAsync(cancellationToken);

        // Start discovery (cancellable via discovery CTS)
        _ = StartDiscoveryAsync(cancellationToken);

        await Task.CompletedTask;
    }

    public async Task<bool> TryStartAutoConnectAsync()
    {
        LogAutoConnectCheckingDefaultProfile(logger);
        var x = await connectionProfileProvider.GetDefaultProfileAsync();
        if (x == null) return false;

        // Connect in the background and let the caller know we are auto connecting
        _ = ConnectAndPersistAsync(x.Id).ContinueWith(t =>
        {
            // If here, we failed to connect. As a fallback,
            // We just load the connection and show them.
            messenger.Send(new ShowToastMessage("Failed to auto-connect to '" + x.Name + "'"));
            LogFailedToAutoConnect(logger, t.Exception);
            _ = RefreshConnectionsAsync();
        }, TaskContinuationOptions.OnlyOnFaulted);
        return true;
    }

    private async Task RefreshConnectionsAsync(CancellationToken externalCancellationToken = default)
    {
        await AbortRefreshAndDiscoveryAsync();

        try
        {
            LogRefreshingConnections(logger);
            _refreshCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
            var cancellationToken = _refreshCancellationTokenSource.Token;

            var connections = await connectionProfileProvider.GetAllProfilesAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var connectionModels = connections
                .Select(ConnectionModel.FromConnectionProfile);

            await GtkDispatch.InvokeIdleAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                View?.RefreshConnections(connectionModels);
            }, cancellationToken);

            LogConnectionsRefreshed(logger);
        }
        catch (Exception e)
        {
            LogFailedToRefreshConnections(e);
        }
    }

    private async Task StartDiscoveryAsync(CancellationToken externalCancellationToken = default)
    {
        await (_discoveryCancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
        _discoveryCancellationTokenSource?.Dispose();
        _discoveryCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);

        var cancellationToken = _discoveryCancellationTokenSource.Token;

        LogDiscovering(logger);

        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.Discovering = true;
        }, cancellationToken);

        try
        {
            await connectionProfileProvider.DiscoverAsync(cancellationToken);
        }
        finally
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.Discovering = false;
            }, cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested) LogDiscoveryAborted(logger);
        else LogDiscoveryCompleted(logger);
    }

    private async void ConnectionProfileProviderOnDiscoveryCompleted(object? sender, EventArgs e)
    {
        try
        {
            await GtkDispatch.InvokeIdleAsync(() => 
            {
                View?.Discovering = false;
            });

            await RefreshConnectionsAsync();
        }
        catch (Exception ex)
        {
            LogFailedToRefreshConnectionsAfterDiscovery(logger, ex);
        }
    }

    private async Task AbortRefreshAndDiscoveryAsync()
    {
        LogAbortingRefreshDiscovery(logger);

        await (_refreshCancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;

        await (_discoveryCancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
        _discoveryCancellationTokenSource?.Dispose();
        _discoveryCancellationTokenSource = null;

        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.Discovering = false;
        });

        LogAbortedRefreshDiscovery(logger);
    }

    private async void ConnectActionHandler(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var connectionId = ParseConnectionId(args);
            await ConnectAndPersistAsync(connectionId);
        }
        catch (Exception e)
        {
            messenger.Send(new ShowToastMessage("Failed to connect"));
            LogFailedToConnectToMPDServer(e);
        }
    }

    private async void ConfigureConnectionHandler(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (View == null) return;

            var connectionId = ParseConnectionId(args);

            var profile = await connectionProfileProvider.GetProfileAsync(connectionId);
            if (profile == null) return;

            var result = await connectDialogPresenter.ShowAsync(View, profile);

            switch (result.Outcome)
            {
                case ConnectDialogOutcome.Cancelled:
                    return;

                case ConnectDialogOutcome.Forgotten:
                    await connectionProfileProvider.DeleteProfileAsync(profile.Id);
                    await RefreshConnectionsAsync();
                    return;

                case ConnectDialogOutcome.Confirmed:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            profile = result.Profile ?? throw new InvalidOperationException("Profile should not be null");

            await SaveAndConnect(profile);
            await RefreshConnectionsAsync();
        }
        catch (Exception e)
        {
            LogFailedToConfigureConnection(logger, e);
        }
    }

    private async void NewConnectionHandler(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            if (View == null) return;

            var profile = connectionProfileFactory.CreateProfile();

            var result = await connectDialogPresenter.ShowAsync(View, profile);

            switch (result.Outcome)
            {
                case ConnectDialogOutcome.Cancelled:
                case ConnectDialogOutcome.Forgotten:
                    return;

                case ConnectDialogOutcome.Confirmed:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            profile = result.Profile ?? throw new InvalidOperationException("Profile should not be null");

            await SaveAndConnect(profile);
            await RefreshConnectionsAsync();
        }
        catch (Exception e)
        {
            LogFailedToCreateNewConnection(logger, e);           
        }
    }

    private async Task ConnectAndPersistAsync(Guid profileId)
    {
        // We can stop refreshing + discovery, we are connecting.
        await AbortRefreshAndDiscoveryAsync();
        
        LogConnectingToProfile(logger, profileId);
        await ariaControl.StartAsync(profileId);

        LogPersistingProfile(logger, profileId);
        await connectionProfileProvider.PersistProfileAsync(profileId);
    }

    private async Task SaveAndConnect(IConnectionProfile profile)
    {
        LogSavingProfile(logger, profile.Id);

        await connectionProfileProvider.SaveProfileAsync(profile);
        await ConnectAndPersistAsync(profile.Id);
    }

    private static Guid ParseConnectionId(SimpleAction.ActivateSignalArgs args)
    {
        if (args.Parameter == null)
            throw new InvalidOperationException("Connection ID parameter missing");

        var guidString = args.Parameter.GetString(out _);
        return Guid.Parse(guidString);
    }

    [LoggerMessage(LogLevel.Error, "Failed to refresh connections")]
    partial void LogFailedToRefreshConnections(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to connect to MPD server")]
    partial void LogFailedToConnectToMPDServer(Exception e);

    [LoggerMessage(LogLevel.Debug, "Auto-connect: checking default profile")]
    static partial void LogAutoConnectCheckingDefaultProfile(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to auto-connect")]
    static partial void LogFailedToAutoConnect(ILogger<WelcomePagePresenter> logger, Exception? e);

    [LoggerMessage(LogLevel.Debug, "Refreshing connections")]
    static partial void LogRefreshingConnections(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Information, "Connections refreshed.")]
    static partial void LogConnectionsRefreshed(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Discovering")]
    static partial void LogDiscovering(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Discovery aborted")]
    static partial void LogDiscoveryAborted(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Information, "Discovery completed")]
    static partial void LogDiscoveryCompleted(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Aborting refresh/discovery")]
    static partial void LogAbortingRefreshDiscovery(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Aborted refresh/discovery")]
    static partial void LogAbortedRefreshDiscovery(ILogger<WelcomePagePresenter> logger);

    [LoggerMessage(LogLevel.Debug, "Connecting to profile {profileId}")]
    static partial void LogConnectingToProfile(ILogger<WelcomePagePresenter> logger, Guid profileId);

    [LoggerMessage(LogLevel.Debug, "Persisting profile {profileId}")]
    static partial void LogPersistingProfile(ILogger<WelcomePagePresenter> logger, Guid profileId);

    [LoggerMessage(LogLevel.Debug, "Saving profile {profileId}")]
    static partial void LogSavingProfile(ILogger<WelcomePagePresenter> logger, Guid profileId);

    [LoggerMessage(LogLevel.Error, "Failed to refresh connections after discovery")]
    static partial void LogFailedToRefreshConnectionsAfterDiscovery(ILogger<WelcomePagePresenter> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to configure connection")]
    static partial void LogFailedToConfigureConnection(ILogger<WelcomePagePresenter> logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to create new connection")]
    static partial void LogFailedToCreateNewConnection(ILogger<WelcomePagePresenter> logger, Exception ex);
}