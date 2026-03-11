using Ebony.Core.Extraction;

namespace Ebony.Backends.MPD.Extraction;

public class PlaylistId(string playlistName) : Id.TypedId<string>(playlistName, Key)
{
    public const string Key = "LST";
    
    public static PlaylistId Parse(string id)
    {
        return new PlaylistId(id);
    }    
}