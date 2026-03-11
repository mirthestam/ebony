namespace Ebony.Core.Queue;

[Flags]
public enum QueueStateChangedFlags
{
    None = 0,
    
    /// <summary>
    /// Indicates that another queue is loaded, and therefore the ID of the current queue has changed
    /// </summary>
    Id = 1 << 0,
    
    /// <summary>
    /// Indicates that the current song, or the upcoming song has changed
    /// </summary>
    PlaybackOrder = 1 << 2,
    
    Shuffle = 1 << 3,
    Repeat = 1 << 4,
    Consume = 1 << 5,
    All = ~0
}