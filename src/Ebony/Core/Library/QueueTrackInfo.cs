using Ebony.Core.Extraction;

namespace Ebony.Core.Library;

public record QueueTrackInfo
{
    /// <summary>
    /// The identity of the entry in the queue.
    /// </summary>
    public required Id Id { get; init; }
    
    /// <summary>
    /// The position of the track in the queue.
    /// </summary>
    public required uint Position { get; init;}
    
    /// <summary>
    /// Represents a track within an album, containing metadata and associated details.
    /// </summary>
    public required TrackInfo Track { get; init; }
}