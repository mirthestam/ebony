using Ebony.Core.Extraction;

namespace Ebony.Backends.MPD.Extraction;

/// <summary>
/// In MPD, players are implemented as partitions.
/// </summary>
public class PlayerId(string name) : Id.TypedId<string>(name, Key)
{
    public const string Key = "PLR";
}