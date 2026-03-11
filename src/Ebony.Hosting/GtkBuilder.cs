using Gio;
using GObject;
using Gtk;
using Action = System.Action;
using Type = System.Type;

namespace Ebony.Hosting;

public class GtkBuilder : IGtkBuilder
{
    public List<Action> GTypeInitializers { get; } = [];
    public string ApplicationId { get; set; } = "";
    public ApplicationFlags ApplicationFlags { get; set; } = ApplicationFlags.FlagsNone;
    public GtkApplicationType GtkApplicationType { get; set; } = GtkApplicationType.Gtk;
    public Type WindowType { get; set; } = typeof(Gtk.ApplicationWindow);
    public Func<IServiceProvider, ApplicationWindow> WindowFactory { get; set; } = _ => throw new InvalidOperationException("Window factory not set."); 
    public List<string> ResourcePaths { get; } = [];
    
    public void WithGType<T>() where T : GTypeProvider
    {
        GTypeInitializers.Add(() => T.GetGType());
    }

    public void WithResource(string resourcePath)
    {
        ResourcePaths.Add(resourcePath);
    }
}