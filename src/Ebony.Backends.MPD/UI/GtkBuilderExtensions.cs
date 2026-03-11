using Ebony.Backends.MPD.UI.Connect;
using Ebony.Hosting;

namespace Ebony.Backends.MPD.UI;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithMPDGTypes()
        {
            builder.WithGType<ConnectDialog>();
        }
    }
}