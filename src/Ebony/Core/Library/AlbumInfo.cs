namespace Ebony.Core.Library;

public sealed record AlbumInfo : Info, IHasAssets
{
    /// <summary>
    /// The Title of the album
    /// </summary>
    public required string Title { get; init; }

    public required DateTime? ReleaseDate { get; init; }
    
    public AlbumCreditsInfo CreditsInfo { get; init; } = new();

    /// <summary>
    /// Optional list of tracks
    /// </summary>
    /// <remarks>Can be empty if this information is not loaded.</remarks>
    public IReadOnlyList<AlbumTrackInfo> Tracks { get; init; } = [];
    
    public IReadOnlyCollection<AssetInfo> Assets { get; init; } = [];
}