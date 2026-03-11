using Ebony.Core.Extraction;

namespace Ebony.Backends.MPD.Extraction;

public class QueueId(int fileName) : Id.TypedId<int>(fileName, Key)
{
    public const string Key = "PLT";    
}