namespace Ebony.Core.Library;

public sealed record ArtistQuery
{
    /// <summary>
    /// If set, only returns artists that have at least one of the specified role flags (OR/ANY semantics).
    /// Example: Composer|Conductor returns artists that are either composer or conductor (or both).
    /// </summary>    
    public ArtistRoles? RequiredRoles { get; init; }

    public ArtistSort Sort { get; init; } = ArtistSort.ByName;
}