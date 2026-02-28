using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Browser.Artists;

public partial class ArtistsPagePresenter : IRecipient<LibraryUpdatedMessage>
{
    private readonly ILogger<ArtistsPagePresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly IAria _aria;
    
    private CancellationTokenSource? _refreshCancellationTokenSource;
    private ArtistsPage? _view;
    
    public ArtistsPagePresenter(ILogger<ArtistsPagePresenter> logger, IMessenger messenger, IAria aria)
    {
        _logger = logger;
        _messenger = messenger;
        _aria = aria;

        messenger.Register(this);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RefreshArtistsAsync(cancellationToken);
    }

    public void Reset()
    {
        try
        {
            AbortRefresh();
            _view?.SetActiveFilter(ArtistsFilter.Featured);
            _view?.RefreshArtists([]);
        }
        catch (Exception e)
        {
            LogFailedToResetArtistsPage(e);
        }
    }

    public void Receive(LibraryUpdatedMessage message)
    {
        if (message.Value.HasFlag(LibraryChangedFlags.Artists))
        {
            _ = RefreshArtistsAsync();
        }
    }

    public void Attach(ArtistsPage view)
    {
        _view = view;
        _view.SetActiveFilter(ArtistsFilter.Featured); // Configurable default in the future?
    }

    public async Task SelectArtist(Id artistId)
    {
        // This artist must be selected in the sidebar.
        await GtkDispatch.InvokeIdleAsync(() => _view?.SelectArtist(artistId));
    }    
    
    private void AbortRefresh()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;
    }

    private async Task RefreshArtistsAsync(CancellationToken externalCancellationToken = default)
    {
        LogRefreshingArtists();
        AbortRefresh();
        
        _refreshCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
        var cancellationToken = _refreshCancellationTokenSource.Token;
        
        try
        {
            var artists = await _aria.Library.GetArtistsAsync(cancellationToken).ConfigureAwait(false);

            if (_view != null)
            {
                await GtkDispatch.InvokeIdleAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    _view.RefreshArtists(artists);
                }, cancellationToken).ConfigureAwait(false);                        
            }

            LogArtistsRefreshed();
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogCouldNotLoadArtists(e);
                _messenger.Send(new ShowToastMessage("Could not load artists"));
            }
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not load artists")]
    partial void LogCouldNotLoadArtists(Exception e);

    [LoggerMessage(LogLevel.Debug, "Refreshing artists")]
    partial void LogRefreshingArtists();

    [LoggerMessage(LogLevel.Debug, "Artists refreshed")]
    partial void LogArtistsRefreshed();

    [LoggerMessage(LogLevel.Error, "Failed to reset artists page")]
    partial void LogFailedToResetArtistsPage(Exception e);
}