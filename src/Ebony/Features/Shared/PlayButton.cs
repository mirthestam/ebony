using Ebony.Core.Player;
using GObject;
using Gtk;

namespace Ebony.Features.Shared;

[Subclass<Button>]
public partial class PlayButton
{
    partial void Initialize()
    {
        SetState(PlaybackState.Stopped);
    }
    
    private const string PlayIconName = "media-playback-start-symbolic";
    private const string PauseIconName = "media-playback-pause-symbolic";
    
    public void SetState(PlaybackState state)
    {
        switch (state)
        {
            case PlaybackState.Unknown:
            case PlaybackState.Stopped:
                TooltipText = "Play";
                IconName = PlayIconName;
                break;                
            case PlaybackState.Playing:
                TooltipText = "Pause";
                IconName = PauseIconName;
                break;
            
            case PlaybackState.Paused:
                TooltipText = "Resume";
                IconName = PlayIconName;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}