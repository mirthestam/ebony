using GObject;
using Gtk;
using Box = Gtk.Box;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(BrowserHost)}.ui")]
public partial class BrowserHost
{
    public enum BrowserState
    {
        Browser,
        EmptyCollection
    }

    private const string EmptyStatePage = "empty-state-page";
    private const string BrowserStatePage = "browser-state-page";
    [Connect("browser-page")] private BrowserPage _browserPage;
    [Connect("browser-state-stack")] private Stack _browserStateStack;

    public BrowserPage BrowserPage => _browserPage;

    public void ToggleState(BrowserState state)
    {
        var pageName = state switch
        {
            BrowserState.Browser => BrowserStatePage,
            BrowserState.EmptyCollection => EmptyStatePage,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        _browserStateStack.VisibleChildName = pageName;
    }
}