using Ebony.Core.Extraction;

namespace Ebony.Backends.MPD.Extraction;

/// <summary>
///     MPD Artists are identified by their name.
/// </summary>
/// <remarks>
///     MPD Unfortunately does not have any kind of disambiguation for artists
/// </remarks>
public class ArtistId(string artistName) : Id.TypedId<string>(artistName, Key)
{
    public const string Key = "ART";
    
    public static Id FromContext(ArtistBaseIdentificationContext context)
    {
        return new ArtistId(context.Artist.Name);
    }

    public static ArtistId Parse(string id)
    {
        return new ArtistId(id);
    }
}