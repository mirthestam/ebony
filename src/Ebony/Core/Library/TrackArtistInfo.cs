namespace Ebony.Core.Library;

public record TrackArtistInfo
{
    public required ArtistInfo Artist { get; init; }

    /// <summary>
    ///     The roles of the artist, as for this track.
    /// </summary>
    /// <example>
    ///     Søren Bebe is both a composer and a performer, as he often performs his own compositions. However, on this
    ///     recording, Lang Lang performs this music. Therefore, for this track, he is credited only as the composer, even
    ///     though, as an artist in general, he is also recognized as a performer.
    /// </example>
    public required ArtistRoles Roles { get; init; } = ArtistRoles.None;
    
    public string? AdditionalInformation { get; init; }
    
    /// <summary>
    /// Denotes the primary or central artist responsible for the performance or recording.
    /// This is what one usually considers the 'artists' of this album.
    /// </summary>
    public bool IsFeatured { get; init; }
}

public record AlbumArtistInfo
{
    public required ArtistInfo Artist { get; init; }
    
    /// <summary>
    ///     The roles of the artist, as for this album.
    /// </summary>
    /// <example>
    ///     If on one track Rachmaninov was a composer, and on another track he was the soloist playing a piece from 
    ///     Chopin, at album level he is credited with both roles: composer and soloist. The album-level roles represent 
    ///     the union of all roles the artist performed across all tracks in the album.
    /// </example>
    public required ArtistRoles Roles { get; init; } = ArtistRoles.None;    
}