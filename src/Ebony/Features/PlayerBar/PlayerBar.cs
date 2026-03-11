using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Player;
using Ebony.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Ebony.Features.PlayerBar;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(PlayerBar)}.ui")]
public partial class PlayerBar
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("elapsed-bar")] private ProgressBar _progressBar;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    [Connect("media-controls")] private Player.MediaControls _mediaControls;
    
    public event EventHandler<Id> EnqueueRequested;    
    
    partial void Initialize()
    {
        // Add the drop target
        var type = GObject.Type.Object;        
        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop += IdWrapperDropTargetOnOnDrop;
        AddController(idWrapperDropTarget);
    }

    public void LoadCoverArt(Art art)
    {
        _coverPicture.Visible = true;
        _coverPicture.SetPaintable(art.Paintable);
    }

    public void ClearCoverArt()
    {
        _coverPicture.Visible = false;
        _coverPicture.SetPaintable(null);
    }    
    
    public void SetCurrentTrack(TrackInfo? trackInfo)
    {
        if (trackInfo == null)
        {
            _titleLabel.Label_ = "";
            _subTitleLabel.Label_ = "";
            _progressBar.Fraction = 0;
            return;
        }
        
        var lines = CurrentTrackHelper.GetLines(trackInfo);
        _titleLabel.Label_ = lines[0];
        _subTitleLabel.Label_ = lines[1];
      }
    
    public void SetPlaybackState(PlaybackState playerState)
    {
        _progressBar.Visible = playerState switch
        {
            PlaybackState.Unknown or PlaybackState.Stopped => false,
            PlaybackState.Playing or PlaybackState.Paused => true,
            _ => false
        };
        
        _mediaControls.SetPlaybackState(playerState);
    }

    public void SetProgress(TimeSpan progressElapsed, TimeSpan progressDuration)
    {
        if (progressDuration == TimeSpan.Zero)
        {
            _progressBar.Fraction = 1;
        }
        else
        {
            _progressBar.Fraction = progressElapsed.TotalSeconds / progressDuration.TotalSeconds;            
        }
    }
    
    private bool IdWrapperDropTargetOnOnDrop(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something onto the mini bar.
        var value = args.Value.GetObject();
        if (value is not GId gId) return false;
        
        EnqueueRequested(this, gId.Id);

        return true;
    }
}