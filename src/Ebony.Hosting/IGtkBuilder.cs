using Gio;
using GObject;
using Type = System.Type;

namespace Ebony.Hosting;

public interface IGtkBuilder
{
    string ApplicationId { get; set; }
    ApplicationFlags ApplicationFlags { get; set; }
    GtkApplicationType GtkApplicationType { get; set; }
    Type WindowType { get; set; }
    Func<IServiceProvider, Gtk.ApplicationWindow>? WindowFactory { get; set; }    
    void WithGType<T>() where T : GTypeProvider;
    void WithResource(string resourcePath);
}