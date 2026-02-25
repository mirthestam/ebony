namespace Aria.Core.Queue;

public enum QueueMode
{
    /// <summary>
    /// The queue is presenting a single album
    /// </summary>
    SingleAlbum,
    
    /// <summary>
    /// The queue is presenting a playlist with songs from (possibly multiple) albums
    /// </summary>
    Playlist
}