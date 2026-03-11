using Gtk;

namespace Ebony.Hosting;

public class GtkContext : IGtkContext
{
    public Application Application { get; set; } = null!;
    public bool IsRunning { get; set; }
}