using Ebony.Features.Shell.Welcome;
using Ebony.Hosting;

namespace Ebony.Features.Shell;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithMainGTypes()
        {
            builder.WithGType<ConnectingPage>();
            builder.WithGType<MainPage>();
            builder.WithGType<WelcomePage>();
            builder.WithGType<ConnectionListItem>();
        }
    }
}