namespace Ebony.Core.Library;

/// <summary>
///     Represents an artist in the library.
/// </summary>
public sealed record ArtistInfo : Info
{
    public required string Name { get; init; }

    public string? NameSort { get; init; }
    
    public string? AdditionalInformation { get; init; }
    
    /// <summary>
    ///     The roles of the artist in the library.
    /// </summary>
    /// <example>
    ///     This library contains music composed by Søren Bebe. However, it also includes tracks in which he performs music
    ///     composed by Chopin. Therefore, as an artist, he is both a performer and a composer.
    /// </example>
    public ArtistRoles Roles { get; init; }
    
    /// <summary>
    /// Denotes the primary or central artist responsible on a performance or recording.
    /// This is what one usually considers the 'artists' of an album.
    /// </summary>
    public bool IsFeatured { get; init; }    
}