using Ebony.Core.Extraction;
using GObject;

namespace Ebony.Infrastructure;

// This class wraps an ID in a GObject.Object,
// allowing it to be used as the content of GTK value objects.
// This makes it straightforward to pass an ID through mechanisms
// such as drag-and-drop operations or GTK actions.

[Subclass<GObject.Object>]
public partial class GId
{
    public Id Id { get; private set; }

    public static GId NewForId(Id id)
    {
        var item = NewWithProperties([]);
        item.Id = id;
        return item;
    }
}