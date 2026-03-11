using Ebony.Hosting;

namespace Ebony.Features.Shared;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithSharedGTypes()
        {
            builder.WithGType<PlayButton>();
        }
    }
}