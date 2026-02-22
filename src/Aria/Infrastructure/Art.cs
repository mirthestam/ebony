using Gdk;

namespace Aria.Infrastructure;

public class Art : IDisposable
{
    public required Paintable Paintable { get; set; }
    public required RGBA[] Palette { get; set; }

    public void Dispose()
    {
        Paintable.Dispose();
    }
}