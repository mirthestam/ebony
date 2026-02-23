using Aria.Core.Player;
using Aria.Features.Shared;
using GObject;
using Gtk;

namespace Aria.Features.Player;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(MediaControls)}.ui")]
public partial class MediaControls
{
    [Connect("playback-start-button")] private PlayButton _playbackStartButton;
    [Connect("skip-backward-button")] private Button _skipBackwardButton;
    [Connect("skip-forward-button")] private Button _skipForwardButton;

    public void SetPlaybackState(PlaybackState playerState)
    {
        _playbackStartButton.SetState(playerState);
    }
}