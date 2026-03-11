using MpcNET;

namespace Ebony.Backends.MPD.Connection.Commands.Playlist;

public class RenameCommand(string playlistName, string newPlaylistName) : IMpcCommand<string>
{
    public string Serialize() => $"rename \"{playlistName}\" \"{newPlaylistName}\"";

    public string Deserialize(SerializedResponse response)
    {
        return string.Join(", ", response.ResponseValues);
    }
}