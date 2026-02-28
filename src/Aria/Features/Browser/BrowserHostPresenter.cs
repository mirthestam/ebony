using Aria.Core;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aria.Features.Browser;

public partial class BrowserHostPresenter : IRootPresenter<BrowserHost>, IRecipient<LibraryUpdatedMessage>
{
    private readonly BrowserPagePresenter _browserPresenter;
    private readonly ILogger<BrowserHostPresenter> _logger;
    private readonly IAria _playerApi;

    public BrowserHostPresenter(ILogger<BrowserHostPresenter> logger,
        IMessenger messenger,
        IAria playerApi,
        BrowserPagePresenter browserPresenter)
    {
        _logger = logger;
        _playerApi = playerApi;
        _browserPresenter = browserPresenter;

        messenger.Register(this);
    }

    public BrowserHost View { get; private set; } = null!;

    public void Attach(BrowserHost view, AttachContext context)
    {
        View = view;
        _browserPresenter.Attach(view.BrowserPage, context);
    }
    
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await DeterminePageAsync(cancellationToken);
        await _browserPresenter.RefreshAsync(cancellationToken);
    }    
    
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
        }, cancellationToken).ConfigureAwait(false);        
        
        await _browserPresenter.ResetAsync().ConfigureAwait(false);
    }
    
    public async void Receive(LibraryUpdatedMessage message)
    {
        try
        {
            if (message.Value != LibraryChangedFlags.None)
            {
                await DeterminePageAsync();
            }
        }
        catch (Exception ex)
        {
            LogFailedToDeterminePageAfterLibraryUpdate(ex);
        }
    }
    
    private async Task DeterminePageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Update the page
            // TODO: Use a helper method for this
            var artists = await _playerApi.Library.GetArtistsAsync(cancellationToken);
            var artistsPresent = artists.Any();
            
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View.ToggleState(artistsPresent
                    ? BrowserHost.BrowserState.Browser
                    : BrowserHost.BrowserState.EmptyCollection);
            }, cancellationToken).ConfigureAwait(false);            
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            if (cancellationToken.IsCancellationRequested) return;
            
            LogCouldNotLoadLibrary(e);
            
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View.ToggleState(BrowserHost.BrowserState.EmptyCollection);
            }, cancellationToken);            
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to determine page after library update")]
    partial void LogFailedToDeterminePageAfterLibraryUpdate(Exception ex);
}