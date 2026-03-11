using Ebony.Core.Library;

namespace Ebony.Core.Extraction;

public sealed class ArtistBaseIdentificationContext : BaseIdentificationContext
{
    /// <summary>
    /// Details about the artist that have been collected so far
    /// </summary>
    public required ArtistInfo Artist { get; init; }
}