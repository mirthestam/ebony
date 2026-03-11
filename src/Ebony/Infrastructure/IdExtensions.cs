using Ebony.Core.Extraction;
using GLib;

namespace Ebony.Infrastructure;

public static class IdExtensions
{
    extension(Id id)
    {
        public Variant ToVariant()
        {
            return Variant.NewString(id.ToString());
        }

        public Variant ToVariantArray()
        {
            return Variant.NewArray(VariantType.String, [id.ToVariant()]);
        }
    }
}