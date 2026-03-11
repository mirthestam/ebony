using Ebony.Core.Extraction;

namespace Ebony.Core.Library;

public sealed record AssetInfo : Info
{
    /// <summary>
    /// The type of the album resource.
    /// </summary>
    public AssetType Type { get; init; }
}