using Ebony.Core.Library;

namespace Ebony.Core.Extraction;

public sealed class AlbumBaseIdentificationContext : BaseIdentificationContext
{
    /// <summary>
    /// Details about the album that have been collected so far
    /// </summary>
    public required AlbumInfo Album { get; init; }
}