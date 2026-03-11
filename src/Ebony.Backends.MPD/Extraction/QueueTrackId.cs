using Ebony.Core.Extraction;
using Ebony.Infrastructure.Extraction;
using MpcNET.Tags;

namespace Ebony.Backends.MPD.Extraction;

public class QueueTrackId(int id) : Id.TypedId<int>(id, Key)
{
    public const string Key = "QUE";
    
    public static Id Parse(string value)
    {
        var id = int.Parse(value);
        return new QueueTrackId(id);
    }

    public static Id FromContext(QueueTrackBaseIdentificationContext context)
    {
        var idString = context.Tags.First(t => t.Name.Equals(MPDTagNames.QueueTags.Id, StringComparison.InvariantCultureIgnoreCase)).Value;
        var id = int.Parse(idString);
        return new QueueTrackId(id);
    }
}