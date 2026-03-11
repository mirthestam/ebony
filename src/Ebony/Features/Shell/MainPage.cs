using Adw;
using Ebony.Core;
using Ebony.Features.Browser;
using Ebony.Infrastructure;
using Gio;
using GObject;
using Gtk;

namespace Ebony.Features.Shell;

#pragma warning disable CS0649
[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(MainPage)}.ui")]
public partial class MainPage
{
    [Connect("player-bar")] private PlayerBar.PlayerBar _playerBar;
    [Connect("browser")] private BrowserHost _browserHost;
    [Connect("player")] private Player.Player _player;
    [Connect("multi-layout-view")] private MultiLayoutView _multiLayoutView;
    [Connect(BottomSheetLayoutName)]private Layout _bottomSheetLayout;
    [Connect(SidebarLayoutName)]private Layout _sidebarLayout;
    
    partial void Initialize()
    {
        // HACK: Force MultiLayoutView to switch layouts to ensure actions are properly bound.
        // Without this, actions (like NextAction and PrevAction) may not be correctly bound to widgets
        // in layouts that haven't been activated yet. Temporarily switching to the other layout and back
        // ensures both layouts have their action bindings initialized properly.
        var currentLayout = _multiLayoutView.GetLayout();
        if (currentLayout == null) return;
        var otherLayout = currentLayout == _sidebarLayout ? _bottomSheetLayout : _sidebarLayout;
        _multiLayoutView.SetLayout(otherLayout);
        _multiLayoutView.SetLayout(currentLayout);        
        
        _cssProvider = CssProvider.New();
        var display = Gdk.Display.GetDefault();
        if (display != null)
        {
            StyleContext.AddProviderForDisplay(display, _cssProvider, 400);
        }        
    }

    private CssProvider _cssProvider;    
    
    public void Colorize(Art? art)
    {
        const string cssClass = "colorized-main-page";
        if (art == null || art.Palette.Length == 0)
        {
            _cssProvider.LoadFromString(string.Empty);
            if (HasCssClass(cssClass)) RemoveCssClass(cssClass);
            return;
        }
        
        var css = new System.Text.StringBuilder();
        css.Append(":root {");

        var colorCount = art.Palette.Length;
        for (var i = 0; i < colorCount; i++)
        {
            var color = art.Palette[i];
            var r = (int)(color.Red * 255);
            var g = (int)(color.Green * 255);
            var b = (int)(color.Blue * 255);
            var a = color.Alpha;

            css.Append($"--background-color-{i}: rgba({r}, {g}, {b}, {a});");
        }
        
        // Fill the remaining required colors
        for (var i = colorCount; i < 5; i++)
        {
            css.Append($"--background-color-{i}: var(--window-bg-color);");
        }

        css.Append('}');
        _cssProvider.LoadFromString(css.ToString());
            
        if (!HasCssClass(cssClass)) AddCssClass(cssClass);
    }
    
    public const string BottomSheetLayoutName = "bottom-sheet-layout";
    public const string SidebarLayoutName = "sidebar-layout";
    
    public PlayerBar.PlayerBar PlayerBar => _playerBar;
    
    public BrowserHost BrowserHost => _browserHost;
    
    public Player.Player Player => _player;
    
    public MultiLayoutView MultiLayoutView => _multiLayoutView;


}