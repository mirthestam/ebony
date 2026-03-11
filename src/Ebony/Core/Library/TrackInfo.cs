using Ebony.Core.Extraction;

namespace Ebony.Core.Library;

public record TrackInfo : Info, IHasAssets
{
    /// <summary>
    ///     The duration  of the track
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    ///     the track title.
    /// </summary>
    public required string Title { get; init; }
    
    public required Id AlbumId { get; init; }

    public TrackCreditsInfo CreditsInfo { get; init; } = new();
    
    public WorkInfo? Work { get; init; } 
    
    public DateTime? ReleaseDate { get; init; }
    
    public string? FileName { get; init; }

    public IReadOnlyCollection<AssetInfo> Assets { get; init; } = [];
}