using Ebony.Core.Library;
using Ebony.Features.Browser.Playlists;

namespace Ebony.Backends.MPD;

public class PlaylistNameValidator : IPlaylistNameValidator
{
    public bool Validate(string name)
    {
        return !string.IsNullOrWhiteSpace(name);
    }
}