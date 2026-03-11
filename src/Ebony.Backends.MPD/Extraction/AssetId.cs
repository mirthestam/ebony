using Ebony.Core.Extraction;

namespace Ebony.Backends.MPD.Extraction;

/// <summary>
/// Represents a unique identifier for a resource.
/// </summary>
public class AssetId(string fileName) : Id.TypedId<string>(fileName, Key)
{
    public const string Key = "AST";
    
    public static Id Parse(string value)
    {
        return new AssetId(value);
    }    
}