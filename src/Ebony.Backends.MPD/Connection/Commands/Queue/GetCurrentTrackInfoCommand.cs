using MpcNET;

namespace Ebony.Backends.MPD.Connection.Commands.Queue;

// ReSharper disable once UnusedType.Global
public class GetCurrentTrackInfoCommand : IMpcCommand<IEnumerable<KeyValuePair<string,string>>>
{
    public string Serialize()
    {
        return "currentsong";
    }

    public IEnumerable<KeyValuePair<string, string>> Deserialize(SerializedResponse response)
    {
        return response.ResponseValues.Count == 0 ? [] : response.ResponseValues;
    }
}