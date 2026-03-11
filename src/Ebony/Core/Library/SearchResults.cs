namespace Ebony.Core.Library;

public sealed record SearchResults
{
    public IReadOnlyList<AlbumInfo> Albums { get; init; } = [];
    public IReadOnlyList<ArtistInfo> Artists { get; init; } = [];
    public IReadOnlyList<TrackInfo> Tracks { get; init; } = [];
    
    public static SearchResults Empty => new();
}