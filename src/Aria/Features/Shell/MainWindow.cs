using Adw;
using Aria.Features.Shell.Welcome;
using Aria.Infrastructure;
using GObject;
using Gtk;
using Microsoft.Extensions.Logging;
using ApplicationWindow = Adw.ApplicationWindow;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Aria.Features.Shell;

[Subclass<ApplicationWindow>]
[Template<AssemblyResource>($"ui/{nameof(MainWindow)}.ui")]
public partial class MainWindow
{
    public enum MainPages
    {
        Main,
        Welcome,
        Connecting
    }

    private ILogger<MainWindow> _logger;

    private const string MainPageName = "main-stack-page";
    private const string WelcomePageName = "welcome-stack-page";
    private const string ConnectingPageName = "connecting-stack-page";
    
    [Connect("main-stack")] private Stack _mainStack;
    [Connect("welcome-page")] private WelcomePage _welcomePage;
    [Connect("connecting-page")] private ConnectingPage _connectingPage;
    [Connect("main-page")] private MainPage _mainPage;

    [Connect("toast-overlay")] private ToastOverlay _toastOverlay;

    public void ShowToast(string message)
    {
        var toast = Toast.New(message);
        toast.SetUseMarkup(false);
        _toastOverlay.AddToast(toast);
    }

    public static MainWindow New(MainWindowPresenter presenter, ILogger<MainWindow> logger)
    {
        var window = NewWithProperties([]);
        
        window._logger = logger;
        presenter.Attach(window);
        
        window.DefaultHeight = 800;
        window.DefaultWidth = 1000;

        // These values don't seem to work then from the .UI file.        
        window.HeightRequest = 294;
        window.WidthRequest = 360;
        
        window.OnRealize += async (s, e) =>
        {
            try
            {
                await presenter.StartupAsync();
            }
            catch (Exception ex)
            {
                window.LogFailedToStartUp(ex);
            }
        };
        

        
        return window;
    }
    
    public WelcomePage WelcomePage => _welcomePage;

    public ConnectingPage ConnectingPage => _connectingPage;

    public MainPage MainPage => _mainPage;
    
    partial void Initialize()
    {
        ConfigureBreakpoints();
    }

    private void ConfigureBreakpoints()
    {
        // Included here in the code because these child objects cannot be located via the .UI XML.
        // Ideally, I would set a child property that recurses, but currently I cannot add properties to GObject.

        // <child>
        //   <object class="AdwBreakpoint">
        //     <condition>max-width: 620sp</condition>
        //     <setter object="multi-layout-view" property="layout-name">bottom-sheet-layout</setter>
        //   </object>
        // </child>
        
        // TODO: Refactor the artists list to use rows when in collapsed view. 
        
        // TODO: In bottom-page layout, the mini player overlaps the artists sidebar.

        var browserBreakpoint = Breakpoint.New(BreakpointCondition.NewLength(BreakpointConditionLengthType.MaxWidth, 860, LengthUnit.Sp));
        browserBreakpoint.AddSetter(MainPage.BrowserHost.BrowserPage.NavigationSplitView, "collapsed", new Value(true));
        AddBreakpoint(browserBreakpoint);
        
        var sidebarBreakpoint = Breakpoint.New(BreakpointCondition.NewLength(BreakpointConditionLengthType.MaxWidth, 660, LengthUnit.Sp));
        sidebarBreakpoint.AddSetter(MainPage.MultiLayoutView, "layout-name", new Value(MainPage.BottomSheetLayoutName));
        sidebarBreakpoint.AddSetter(MainPage.BrowserHost.BrowserPage.NavigationSplitView, "collapsed", new Value(true));        
        AddBreakpoint(sidebarBreakpoint);
     }

    public void TogglePage(MainPages page)
    {
        var pageName = page switch
        {
            MainPages.Welcome => WelcomePageName,
            MainPages.Connecting => ConnectingPageName,
            MainPages.Main => MainPageName,
            _ => throw new ArgumentOutOfRangeException(nameof(page), page, null)
        };

        _mainStack.VisibleChildName = pageName;
    }

    [LoggerMessage(LogLevel.Critical, "Failed to start up")]
    partial void LogFailedToStartUp(Exception ex);
}