using Ebony.Features.Player.Queue;
using Ebony.Hosting;

namespace Ebony.Features.Details;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithDetailsGTypes()
        {
            builder.WithGType<TrackDetailsDialog>();
        }
    }
}