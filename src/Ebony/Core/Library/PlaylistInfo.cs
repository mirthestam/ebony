namespace Ebony.Core.Library;

public sealed record PlaylistInfo : Info
{
    public required string Name { get; init; }
    
    public IReadOnlyList<AlbumTrackInfo> Tracks { get; init; } = [];
    
    public DateTime LastModified { get; init; }
}