namespace Aria.Core.Library;

[Flags]
public enum LibraryChangedFlags
{
    None = 0,

    /// <summary>
    /// Playlists (Stored)
    /// </summary>
    Playlists = 1 << 1,
    
    Artists = 1 << 0,
    
    Albums = 1 << 2,
    
    Tracks = 1 << 3
}