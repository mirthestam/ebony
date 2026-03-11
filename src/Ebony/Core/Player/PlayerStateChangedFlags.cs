namespace Ebony.Core.Player;

[Flags]
public enum PlayerStateChangedFlags
{
    None = 0,
    
    /// <summary>
    /// Indicates that the playback state has changed.
    /// </summary>
    /// <example>Playing, Paused, Stopped</example>
    PlaybackState = 1 << 0,
    
    Volume = 1 << 1,
    
    /// <summary>
    /// Indicates that the progress (elapsed time) of the current track has changed.
    /// </summary>
    Progress = 1 << 2,
    
    XFade = 1 << 3,
    
    All = ~0
}