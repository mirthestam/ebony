namespace Ebony.Core.Library;

public record AlbumTrackInfo  : Info
{
    /// <summary>
    /// Represents a track within an album, containing metadata and associated details.
    /// </summary>
    public required TrackInfo Track { get; init; }

    /// <summary>
    /// The track number of the track within the volume.
    /// </summary>
    public required int? TrackNumber { get; init; }

    /// <summary>
    /// Gets the identifier of the specific media volume or side where this track is located.
    /// </summary>
    /// <remarks>
    /// This property provides a flexible way to group tracks based on their physical or logical grouping 
    /// within a release. While digital releases might use a single volume, physical media often 
    /// require more granular identification:
    /// <list type="bullet">
    ///   <item><description><b>CDs:</b> Usually represented by numeric disc indices (e.g., "1", "2").</description></item>
    ///   <item><description><b>Vinyl &amp; Tapes:</b> Typically represented by sides (e.g., "A", "B").</description></item>
    ///   <item><description><b>Box Sets:</b> May use descriptive names for bonus or thematic discs (e.g., "Bonus", "Live", "Blu-ray").</description></item>
    /// </list>
    /// </remarks>
    /// <example>"1", "2"</example>
    /// <example>"A", "B"</example>
    /// <example>"Bonus Disc"</example>
    /// 
    public required string? VolumeName { get; init; }
    
    public TrackGroup? Group { get; init; }
}
