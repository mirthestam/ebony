using Gtk;

namespace Ebony.Hosting;

public interface IGtkContext
{
    Application Application { get; set; }
    bool IsRunning { get; set; }
}