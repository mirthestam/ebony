using MpcNET;

namespace Ebony.Backends.MPD.Connection.Commands.Playlist;

public class SaveCommand(
    string playlistName,
    SaveCommand.SaveMethod method = SaveCommand.SaveMethod.Create)
    : IMpcCommand<string>
{
    public enum SaveMethod
    {
        /// <summary>
        /// Create a new playlist. Fail if a playlist with name already exists.
        /// </summary>
        Create,

        /// <summary>
        /// Append to an existing playlist. Fail if a playlist with name doesn't already exist.
        /// </summary>
        Append,

        /// <summary>
        /// Replace an existing playlist. Fail if a playlist with name NAME doesn't already exist.
        /// </summary>
        Replace
    }

    public string Serialize()
    {
        var methodCommand = method switch
        {
            SaveMethod.Create => "create",
            SaveMethod.Append => "append",
            SaveMethod.Replace => "replace",
            _ => throw new ArgumentOutOfRangeException()
        };

        return $"save \"{playlistName}\" {methodCommand}";
    }

    /// <summary>
    /// Deserializes the specified response text pairs.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>
    /// The deserialized response.
    /// </returns>
    public string Deserialize(SerializedResponse response)
    {
        return string.Join(", ", response.ResponseValues);
    }
}