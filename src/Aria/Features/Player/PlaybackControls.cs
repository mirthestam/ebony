using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Features.Player.Queue;
using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;
using Box = Gtk.Box;
using Range = Gtk.Range;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(PlaybackControls)}.ui")]
public partial class PlaybackControls
{
    [Connect("elapsed-scale")] private Scale _elapsedScale;
    [Connect("elapsed-time-label")] private Label _elapsedTimeLabel;
    [Connect("media-controls")] private MediaControls _mediaControls;
    [Connect("playlist-progress-label")] private Label _playlistProgressLabel;
    [Connect("remaining-time-label")] private Label _remainingTimeLabel;
    [Connect("tech-label")] private Label _techLabel;

    [Connect("elapsed-popover")] private Popover _elapsedPopover;
    [Connect("elapsed-popover-label")] private Label _elapsedPopoverLabel;

    [Connect("volume-scale-button")] private ScaleButton _volumeButton;
    
    [Connect("track-item")] private TrackListItem _trackListItem;

    private EventControllerMotion _motionController;
    private TimeSpan _shownDuration;
    
    private CancellationTokenSource? _seekCts;

    public event SeekRequestedAsyncHandler? SeekRequested;
    public event EventHandler<int>? VolumeChanged;
    
    partial void Initialize()
    {
        _motionController = EventControllerMotion.NewWithProperties([]);
        _elapsedScale.AddController(_motionController);
        _elapsedScale.OnChangeValue += ElapsedScaleOnOnChangeValue;

        _elapsedPopover.SetParent(_elapsedScale);

        _motionController.OnMotion += MotionControllerOnOnMotion;
        _motionController.OnLeave += MotionControllerOnOnLeave;

        _volumeButton.OnValueChanged += VolumeButtonOnValueChanged;
    }

    private bool ElapsedScaleOnOnChangeValue(Range sender, Range.ChangeValueSignalArgs args)
    {
        // The range may fall outside the scrollbar bounds, so we clamp it.
        var seconds = Math.Clamp(args.Value, 0, _shownDuration.TotalSeconds);        
        
        var target = TimeSpan.FromSeconds(seconds);

        _seekCts?.Cancel();
        _seekCts?.Dispose();
        _seekCts = new CancellationTokenSource();
        var ct = _seekCts.Token;

        _ = SeekRequested?.Invoke(target, ct) ?? Task.CompletedTask;

        // This false allows GTK to update the slider
        return false;
    }
    
    private void VolumeButtonOnValueChanged(ScaleButton sender, ScaleButton.ValueChangedSignalArgs args)
    {
        var volume = (int)Math.Round(args.Value);
        VolumeChanged?.Invoke(this, volume);
    }

    private void MotionControllerOnOnLeave(EventControllerMotion sender, EventArgs args)
    {
        _elapsedPopover.Popdown();
    }

    private void MotionControllerOnOnMotion(EventControllerMotion sender, EventControllerMotion.MotionSignalArgs args)
    {
        try
        {
            // Calculate the hovered time
            _elapsedScale.GetRangeRect(out var rangeRect);
            var duration = _elapsedScale.Adjustment!.Upper;

            var x = Math.Clamp(args.X, 0, rangeRect.Width);

            var isRtl = _elapsedScale.GetDirection() == TextDirection.Rtl;
            var elapsedSeconds = isRtl
                ? (rangeRect.Width - x) / rangeRect.Width * duration
                : x / rangeRect.Width * duration;

            elapsedSeconds = Math.Clamp(elapsedSeconds, 0, duration);
            var timeSpan = TimeSpan.FromSeconds(elapsedSeconds);

            // Update the label with the formatted time
            _elapsedPopoverLabel.Label_ = timeSpan.ToString(@"mm\:ss");

            // (Re)position the popover
            const int yOffset = 12;
            var rect = new Rectangle
            {
                X = (int)Math.Round(rangeRect.X + x),
                Y = (int)Math.Round((double)(rangeRect.Y - yOffset)),
                Width = 1,
                Height = 1
            };

            _elapsedPopover.SetPointingTo(rect);

            if (!_elapsedPopover.Visible)
                _elapsedPopover.Popup();
        }
        catch
        {
            _elapsedPopover.Popdown();
        }
    }

    public void SetCurrentTrack(QueueTrackInfo? trackInfo)
    {
        _trackListItem.Visible = trackInfo != null;
        
        if (trackInfo == null) return;
        
        // TODO: Maybe make a separate widget instead of reusing the QueueTrackModel
        // if it gets modified too much
        var queueModel = QueueModel.NewWithProperties([]);
        queueModel.Mode = QueueMode.Playlist;
        
        var model = QueueTrackModel.NewFromQueueTrackInfo(trackInfo, queueModel);
        _trackListItem.Bind(model);
    }
    
    public void SetProgress(PlaybackProgress progress)
    {
        _elapsedTimeLabel.Label_ = progress.Elapsed.ToString(@"mm\:ss");
        
        if (progress.Duration == TimeSpan.Zero)
        {
            if (_shownDuration == progress.Duration) return;
            _elapsedScale.Visible = false;                            
            _shownDuration = progress.Duration;
            _remainingTimeLabel.Label_ = "—:—";
        }
        else
        {
            if (_shownDuration != progress.Duration)
            {
                _elapsedScale.Visible = true;                
                _elapsedScale.SetRange(0, progress.Duration.TotalSeconds);
                _shownDuration = progress.Duration;
            }

            _elapsedScale.SetValue(progress.Elapsed.TotalSeconds);
            _remainingTimeLabel.Label_ =(progress.Duration - progress.Elapsed).ToString(@"mm\:ss");
            _techLabel.Label_ = $"{progress.AudioBits}-bit / {progress.AudioSampleRate / 1000.0:F1} kHz";
        }
    }

    public void SetPlaylistInfo(uint? playlistCurrentTrackIndex, uint playlistLength)
    {
        var hasLength = playlistLength > 0;

        if (_playlistProgressLabel.Visible != hasLength)
        {
            _playlistProgressLabel.Visible = hasLength;
        }

        _playlistProgressLabel.Label_ = hasLength
            ? $"{playlistCurrentTrackIndex + 1}/{playlistLength}"
            : "0/0";
    }

    public void SetPlaybackState(PlaybackState playerState)
    {
        _elapsedScale.Visible = playerState switch
        {
            PlaybackState.Unknown or PlaybackState.Stopped => false,
            PlaybackState.Playing or PlaybackState.Paused => true,
            _ => _elapsedScale.Visible
        };

        _mediaControls.SetPlaybackState(playerState);
    }

    public void SetVolume(int? playerVolume)
    {
        _volumeButton.Visible = playerVolume.HasValue;
        _volumeButton.SetValue(playerVolume?? 0);
    }

    public void SetCoverArt(Art? art)
    {
        _trackListItem.Model?.CoverArt = art;
    }
}