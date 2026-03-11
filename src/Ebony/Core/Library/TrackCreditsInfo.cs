namespace Ebony.Core.Library;

public record TrackCreditsInfo
{
    public IReadOnlyList<TrackArtistInfo> Artists { get; init; } = [];

    public IReadOnlyList<AlbumArtistInfo> AlbumArtists { get; init; } = [];
}