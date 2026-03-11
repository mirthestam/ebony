namespace Ebony.Core.Library;

public record AlbumCreditsInfo
{
    /// <summary>
    ///     Information of all artists somehow participating on this album.
    ///     And, what they did. Is a sum or al tracks, and differs per actual track.
    /// </summary>
    /// <remarks>Can be empty if this information is not loaded.</remarks>
    public IReadOnlyList<TrackArtistInfo> Artists { get; init; } = [];

    /// <summary>
    ///     The Album Artists. The same for all tracks on this album
    /// </summary>
    public IReadOnlyList<AlbumArtistInfo> AlbumArtists { get; init; } = [];
}