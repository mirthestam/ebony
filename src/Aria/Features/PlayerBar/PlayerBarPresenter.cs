using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.PlayerBar;

public partial class PlayerBarPresenter : IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly ILogger<PlayerBarPresenter> _logger;
    private readonly IMessenger _messenger;
    private readonly ArtAssetLoader _artAssetLoader;
    private Art? _currentCoverArt;
    private PlayerBar? _view;

    private CancellationTokenSource? _coverArtCancellationTokenSource;

    public PlayerBarPresenter(IAria aria, IMessenger messenger, ILogger<PlayerBarPresenter> logger,
        ArtAssetLoader artAssetLoader)
    {
        _logger = logger;
        _artAssetLoader = artAssetLoader;
        _aria = aria;
        _messenger = messenger;
        messenger.Register<PlayerStateChangedMessage>(this);
        messenger.Register<QueueStateChangedMessage>(this);
    }

    public void Attach(PlayerBar bar)
    {
        _view = bar;
        _view.EnqueueRequested += ViewOnEnqueueRequested;
    }

    private async void ViewOnEnqueueRequested(object? sender, Id id)
    {
        try
        {
            var info = await _aria.Library.GetItemAsync(id);
            if (info == null) return;

            _ = _aria.Queue.EnqueueAsync(info, IQueue.DefaultEnqueueAction);
        }
        catch (Exception exception)
        {
            _messenger.Send(new ShowToastMessage("Could not enqueue"));
            LogCouldNotEnqueue(exception);
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RefreshCoverAsync(cancellationToken);

        await RefreshAsync(QueueStateChangedFlags.All, cancellationToken);
        await RefreshAsync(PlayerStateChangedFlags.All, cancellationToken);

        await Task.CompletedTask;
    }

    public async Task ResetAsync()
    {
        AbortRefreshCover();

        await GtkDispatch.InvokeIdleAsync(() => { _view?.ClearCoverArt(); });

        _currentCoverArt?.Dispose();
        _currentCoverArt = null;
    }

    public async void Receive(PlayerStateChangedMessage message)
    {
        try
        {
            await RefreshAsync(message.Value);
        }
        catch (Exception ex)
        {
            LogFailedToHandlePlayerStateChange(ex);
        }
    }

    public async void Receive(QueueStateChangedMessage message)
    {
        try
        {
            await RefreshAsync(message.Value);
        }
        catch (Exception ex)
        {
            LogFailedToHandleQueueStateChange(ex);
        }
    }

    private async Task RefreshAsync(PlayerStateChangedFlags flags, CancellationToken cancellationToken = default)
    {
        if (flags.HasFlag(PlayerStateChangedFlags.PlaybackState))
        {
            await GtkDispatch.InvokeIdleAsync(() => { _view?.SetPlaybackState(_aria.Player.State); },
                cancellationToken);
        }

        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
        {
            await GtkDispatch.InvokeIdleAsync(
                () => { _view?.SetProgress(_aria.Player.Progress.Elapsed, _aria.Player.Progress.Duration); },
                cancellationToken);
        }
    }

    private async Task RefreshAsync(QueueStateChangedFlags flags, CancellationToken cancellationToken = default)
    {
        if (!flags.HasFlag(QueueStateChangedFlags.Id) && !flags.HasFlag(QueueStateChangedFlags.PlaybackOrder)) return;

        if (_aria.Queue.Length == 0)
        {
            // The queue has changed and is empty.
            await GtkDispatch.InvokeIdleAsync(() => { _view?.SetCurrentTrack(null); }, cancellationToken);
        }
        else
        {
            var track = _aria.Queue.CurrentTrack;

            await GtkDispatch.InvokeIdleAsync(() => { _view?.SetCurrentTrack(track?.Track); }, cancellationToken);
        }

        await RefreshCoverAsync(cancellationToken);
    }

    private void AbortRefreshCover()
    {
        _coverArtCancellationTokenSource?.Cancel();
        _coverArtCancellationTokenSource?.Dispose();
        _coverArtCancellationTokenSource = null;
    }

    private async Task RefreshCoverAsync(CancellationToken externalCancellationToken = default)
    {
        AbortRefreshCover();

        _coverArtCancellationTokenSource = externalCancellationToken != CancellationToken.None
            ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken)
            : new CancellationTokenSource();

        var cancellationToken = _coverArtCancellationTokenSource.Token;

        try
        {
            var track = _aria.Queue.CurrentTrack;
            if (track == null)
            {
                await GtkDispatch.InvokeIdleAsync(() => 
                { 
                    if (cancellationToken.IsCancellationRequested) return;
                    _view?.ClearCoverArt(); 
                }, cancellationToken);

                _currentCoverArt?.Dispose();
                _currentCoverArt = null;

                return;
            }

            var coverInfo = track.Track.Assets.FrontCover;
            var newCoverArt = await _artAssetLoader.LoadFromAssetAsync(coverInfo?.Id ?? Id.Empty, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            if (newCoverArt == null) return;

            var previousCoverArt = _currentCoverArt;
            _currentCoverArt = newCoverArt;

            await GtkDispatch.InvokeIdleAsync(() => 
            { 
                if (cancellationToken.IsCancellationRequested) return;
                _view?.LoadCoverArt(newCoverArt); 
            }, cancellationToken);

            previousCoverArt?.Dispose();
        }
        catch (OperationCanceledException)
        {
            // Expected when a new cover starts loading
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested) LogFailedToLoadAlbumCover(e);
        }
    }

    [LoggerMessage(LogLevel.Error, "Could not enqueue")]
    partial void LogCouldNotEnqueue(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to handle player state change")]
    partial void LogFailedToHandlePlayerStateChange(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to handle queue state change")]
    partial void LogFailedToHandleQueueStateChange(Exception e);
}