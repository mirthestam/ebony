using Aria.Core;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Queue;
using Aria.Features.Shell;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Gdk;
using GLib;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using TimeSpan = System.TimeSpan;

namespace Aria.Features.Player;

public partial class PlayerPresenter : IRootPresenter<Player>,  IRecipient<PlayerStateChangedMessage>, IRecipient<QueueStateChangedMessage>
{
    private readonly IAria _aria;
    private readonly IAriaControl _ariaControl;
    private readonly ILogger<PlayerPresenter> _logger;
    private readonly QueuePresenter _queuePresenter;
    private readonly ArtAssetLoader _artAssetLoader;
    private readonly IMessenger _messenger;
    private readonly IPlaylistNameValidator _playlistNameValidator;
    
    private CancellationTokenSource? _coverArtCancellationTokenSource;
    private Art? _currentCoverArt;    
    
    public Player? View { get; private set; }
    
    public PlayerPresenter(ILogger<PlayerPresenter> logger, IMessenger messenger, IAria aria,
        ArtAssetLoader artAssetLoader, QueuePresenter queuePresenter, IAriaControl ariaControl, IPlaylistNameValidator playlistNameValidator)
    {
        _messenger = messenger;
        _logger = logger;
        _artAssetLoader = artAssetLoader;
        _queuePresenter = queuePresenter;
        _ariaControl = ariaControl;
        _playlistNameValidator = playlistNameValidator;
        _aria = aria;
        messenger.RegisterAll(this);
    }
    
    public void Attach(Player player, AttachContext context)
    {
        View = player;
        View.SeekRequested += ViewOnSeekRequested;
        View.EnqueueRequested += ViewOnEnqueueRequested;
        View.VolumeChanged += ViewOnVolumeChanged;

        _queuePresenter.Attach(View.Queue);
        
        
        InitializeActions(context);
    }

    private async void ViewOnVolumeChanged(object? sender, int e)
    {
        try
        {
            await _aria.Player.SetVolumeAsync(e);
        }
        catch(Exception ex)
        {
            LogFailedToSetVolume(ex);
        }
    }

    private async Task ViewOnSeekRequested(TimeSpan position, CancellationToken cancellationToken)
    {
        await _aria.Player.SeekAsync(position, cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(QueueStateChangedFlags.All, cancellationToken);
        await RefreshAsync(PlayerStateChangedFlags.All, cancellationToken);

        await _queuePresenter.RefreshAsync(cancellationToken);
        await RefreshCoverAsync(cancellationToken);        
    }

    public async Task ResetAsync()
    {
        await _queuePresenter.ResetAsync();
        AbortRefreshCover();
        
        await GtkDispatch.InvokeIdleAsync(() => 
        {
            View?.ClearCoverArt();
        });
        
        _currentCoverArt?.Dispose();
        _currentCoverArt = null;        
    }
    
    public async void Receive(PlayerStateChangedMessage message)
    {
        try
        {
            await RefreshAsync(message.Value);
        }
        catch(Exception ex)
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
        catch(Exception ex)
        {
            LogFailedToHandleQueueStateChange(ex);
        }
    }

    private async Task RefreshAsync(PlayerStateChangedFlags flags, CancellationToken cancellationToken = default)
    {
        if (flags.HasFlag(PlayerStateChangedFlags.PlaybackState))
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.SetPlaybackState(_aria.Player.State);
            }, cancellationToken);            
        }
        if (flags.HasFlag(PlayerStateChangedFlags.Progress))
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.SetProgress(_aria.Player.Progress);
            }, cancellationToken);            
        }

        if (flags.HasFlag(PlayerStateChangedFlags.Volume))
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.SetVolume(_aria.Player.Volume);
            }, cancellationToken);                        
        }
    }

    private async Task RefreshAsync(QueueStateChangedFlags flags, CancellationToken cancellationToken = default)
    {
        // Some changes implicitly effect playback order.
        var refreshPlaybackOrder = false;
        
        if (flags.HasFlag(QueueStateChangedFlags.Shuffle))
        {
            _ariaQueueShuffleAction.Enabled = _aria.Queue.Shuffle.Supported;
            _ariaQueueShuffleAction.SetState(Variant.NewBoolean(_aria.Queue.Shuffle.Enabled));
            refreshPlaybackOrder = true;
        }

        if (flags.HasFlag(QueueStateChangedFlags.Repeat))
        {
            _ariaQueueRepeatAction.Enabled = _aria.Queue.Repeat.Supported;
            _ariaQueueRepeatAction.SetState(Variant.NewString(_aria.Queue.Repeat.Mode.ToString()));
            refreshPlaybackOrder = true;
        }
        
        if (flags.HasFlag(QueueStateChangedFlags.Consume))
        {
            _ariaQueueConsumeAction.Enabled = _aria.Queue.Consume.Supported;
            _ariaQueueConsumeAction.SetState(Variant.NewBoolean(_aria.Queue.Consume.Enabled));
            refreshPlaybackOrder = true;
        }
        
        if (flags.HasFlag(QueueStateChangedFlags.Id) || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {

            if (_aria.Queue.Length == 0)
            {
                await GtkDispatch.InvokeIdleAsync(() => 
                {
                    View?.SetCurrentTrack(null, QueueMode.Playlist);
                }, cancellationToken);                
            }
            else
            {
                var track = _aria.Queue.CurrentTrack;
                await GtkDispatch.InvokeIdleAsync(() =>
                {
                    View?.SetCurrentTrack(track, _aria.Queue.Mode);
                }, cancellationToken);
            }
            
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.SetPlaylistInfo(_aria.Queue.Order.CurrentIndex, _aria.Queue.Length);
            }, cancellationToken);            
        }
        
        if (refreshPlaybackOrder || flags.HasFlag(QueueStateChangedFlags.PlaybackOrder))
        {
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.SetQueueMode(_aria.Queue.Mode);                
                _ariaPlayerPreviousTrackAction.SetEnabled(_aria.Queue.Order.CurrentIndex > 0);
                _ariaPlayerNextTrackAction.SetEnabled(_aria.Queue.Order.HasNext);
                _ariaPlayerPlayPauseAction.SetEnabled(_aria.Queue.Length > 0);
            }, cancellationToken);
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
        
        // Create a new cancellation token source that is optionally linked to an external token.
        // This allows cover loading to be cancelled both internally (e.g., when a new track starts)
        // and externally (e.g., when the component cancels connection via the cancellation token passed to ConnectAsync).
        // The linked token ensures that cancelling either source will cancel the cover loading operation.
        _coverArtCancellationTokenSource = externalCancellationToken != CancellationToken.None 
            ? CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken) 
            : new CancellationTokenSource();
            
        var cancellationToken = _coverArtCancellationTokenSource.Token;            
        
        try
        {
            AssetInfo? coverInfo = null;
            
            switch (_aria.Queue.Mode)
            {
                case QueueMode.SingleAlbum:
                    var firstTrack = _aria.Queue.Tracks.FirstOrDefault();
                    if (firstTrack != null)
                    {
                        coverInfo = firstTrack.Track.Assets.FrontCover;
                    }
                    break;
                
                case QueueMode.Playlist:
                    var track = _aria.Queue.CurrentTrack;
                    if (track != null)
                    {
                        coverInfo = track.Track.Assets.FrontCover;
                    }
                
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (coverInfo == null)
            {
                await GtkDispatch.InvokeIdleAsync(() =>
                {
                    View?.ClearCoverArt();
                    _currentCoverArt?.Dispose();                    
                }, cancellationToken);
                
                _currentCoverArt = null;

                return;
            }
            
            //var texture = await _resourceTextureLoader.LoadFromAlbumResourceAsync(coverInfo?.Id ?? Id.Empty, cancellationToken).ConfigureAwait(false);
            var newCoverArt = await Task.Run(
                () => _artAssetLoader.LoadFromAssetAsync(coverInfo?.Id ?? Id.Empty, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) return;
            if (newCoverArt == null) return;
            
            var previousCoverArt = _currentCoverArt;
            _currentCoverArt = newCoverArt;            
            
            await GtkDispatch.InvokeIdleAsync(() =>
            {
                View?.LoadCoverArt(newCoverArt);
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
    
    [LoggerMessage(LogLevel.Error, "Failed to load album cover")]
    partial void LogFailedToLoadAlbumCover(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to set volume")]
    partial void LogFailedToSetVolume(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to handle player state change")]
    partial void LogFailedToHandlePlayerStateChange(Exception ex);

    [LoggerMessage(LogLevel.Error, "Failed to handle queue state change")]
    partial void LogFailedToHandleQueueStateChange(Exception ex);
}