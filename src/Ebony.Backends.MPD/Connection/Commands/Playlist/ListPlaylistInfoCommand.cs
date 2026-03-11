using MpcNET;

namespace Ebony.Backends.MPD.Connection.Commands.Playlist;

public class ListPlaylistInfoCommand : IMpcCommand<IEnumerable<KeyValuePair<string, string>>>
{
    private readonly string playlistName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPlaylistInfoCommand"/> class.
    /// </summary>
    /// <param name="playlistName">Name of the playlist.</param>
    public ListPlaylistInfoCommand(string playlistName)
    {
        this.playlistName = playlistName;
    }

    /// <summary>
    /// Serializes the command.
    /// </summary>
    /// <returns>
    /// The serialize command.
    /// </returns>
    public string Serialize() => string.Join(" ", "listplaylistinfo", $"\"{this.playlistName}\"");

    /// <summary>
    /// Deserializes the specified response text pairs.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>
    /// The deserialized response.
    /// </returns>
    public IEnumerable<KeyValuePair<string, string>> Deserialize(SerializedResponse response)
    {
        return response.ResponseValues;
    }
}