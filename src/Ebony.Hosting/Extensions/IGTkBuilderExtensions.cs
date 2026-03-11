using Gtk;

namespace Ebony.Hosting.Extensions;

public static class IGTkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public IGtkBuilder UseWindow<TWindow>(Func<IServiceProvider, TWindow> factory)
            where TWindow : ApplicationWindow
        {
            builder.WindowType = typeof(TWindow);
            builder.WindowFactory = factory;
            return builder;
        }
    }
}