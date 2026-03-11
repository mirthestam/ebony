using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Player;
using Ebony.Core.Queue;
using Ebony.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Ebony.Features.Player;

public delegate Task SeekRequestedAsyncHandler(TimeSpan position, CancellationToken cancellationToken);

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(Player)}.ui")]
public partial class Player
{
    [Connect("album-picture")] private Picture _coverPicture;
    [Connect("coverart-revealer")] private Revealer _coverArtRevealer;
    
    [Connect("playback-controls")] private PlaybackControls _playbackControls;
    [Connect("playlist")] private Queue.Queue _queue;
    
    public Queue.Queue Queue => _queue;

    public event SeekRequestedAsyncHandler? SeekRequested;    
    public event EventHandler<int>? VolumeChanged;
    
    public event EventHandler<Id> EnqueueRequested;    
    
    partial void Initialize()
    {
        _playbackControls.SeekRequested += PlaybackControlsOnSeekRequested;
        _playbackControls.VolumeChanged += PlaybackControlsOnVolumeChanged;
        
        // Add the playback drop target
        var type = GObject.Type.Object;        
        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop  += IdWrapperDropTargetOnOnDrop;
        AddController(idWrapperDropTarget);
    }

    private void PlaybackControlsOnVolumeChanged(object? sender, int e) => VolumeChanged?.Invoke(sender, e);

    private bool IdWrapperDropTargetOnOnDrop(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something onto the player.
        var value = args.Value.GetObject();
        if (value is not GId gId) return false;
        
        EnqueueRequested(this, gId.Id);

        return true;
    }

    private Task PlaybackControlsOnSeekRequested(TimeSpan position, CancellationToken cancellationToken)
    {
        return SeekRequested?.Invoke(position, cancellationToken) ?? Task.CompletedTask;
    }
    
    public void LoadCoverArt(Art? art)
    {
        _playbackControls.SetCoverArt(art);
        _coverPicture.SetPaintable(art?.Paintable);
    }

    public void ClearCoverArt()
    {
        _playbackControls.SetCoverArt(null);        
        _coverPicture.SetPaintable(null);
    }

    public void SetProgress(PlaybackProgress progress)
    {
        _playbackControls.SetProgress(progress);
    }

    public void SetPlaylistInfo(uint? orderCurrentIndex, uint queueLength)
    {
        _playbackControls.SetPlaylistInfo(orderCurrentIndex, queueLength);
    }

    public void SetPlaybackState(PlaybackState playerState)
    {
        _playbackControls.SetPlaybackState(playerState);
    }

    public void SetVolume(int? playerVolume)
    {
        _playbackControls.SetVolume(playerVolume);
    }

    public void SetCurrentTrack(QueueTrackInfo? trackTrack, QueueMode queueMode)
    {
        _playbackControls.SetCurrentTrack(trackTrack, queueMode);
    }

    public void SetQueueMode(QueueMode queueMode)
    {
        var reveal =queueMode switch
        {
            QueueMode.SingleAlbum => true,
            _ => false
        };
        
        if (_coverArtRevealer.ChildRevealed != reveal)
        {
            _coverArtRevealer.SetRevealChild(reveal);
        }
    }
}