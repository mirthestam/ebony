using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Features.Shell.Welcome;
using Aria.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Application = Adw.Application;
using Task = System.Threading.Tasks.Task;

namespace Aria.Features.Shell;

public partial class MainWindowPresenter : IRecipient<ShowToastMessage>
{
    private readonly Application _application;
    private readonly ILogger<MainWindowPresenter> _logger;
    private readonly IAria _aria;
    private readonly IAriaControl _ariaControl;
    private readonly MainPagePresenter _mainPagePresenter;
    private readonly WelcomePagePresenter _welcomePagePresenter;
    private readonly ArtAssetLoader _artAssetLoader;

    public MainWindow View { get; private set; }

    public MainWindowPresenter(IMessenger messenger,
        MainPagePresenter mainPagePresenter,
        WelcomePagePresenter welcomePagePresenter,
        ILogger<MainWindowPresenter> logger,
        Application application,
        IAria aria,
        IAriaControl ariaControl, ArtAssetLoader artAssetLoader)
    {
        _application = application;
        _aria = aria;
        _mainPagePresenter = mainPagePresenter;
        _welcomePagePresenter = welcomePagePresenter;
        _ariaControl = ariaControl;
        _artAssetLoader = artAssetLoader;
        _logger = logger;

        messenger.RegisterAll(this);

        _ariaControl.StateChanged += AriaControlOnStateChanged;
    }

    private void AriaControlOnStateChanged(object? sender, EngineStateChangedEventArgs e)
    {
        GtkDispatch.InvokeIdle(() =>
        {
            switch (e.State)
            {
                case EngineState.Stopped:
                    View.TogglePage(MainWindow.MainPages.Welcome);
                    _ = _welcomePagePresenter.RefreshAsync();
                    break;
                case EngineState.Starting:
                    View.TogglePage(MainWindow.MainPages.Connecting);
                    break;
                case EngineState.Seeding:
                    // Ignore seeding state
                    break;
                case EngineState.Ready:
                    View.TogglePage(MainWindow.MainPages.Main);
                    break;
                case EngineState.Stopping:
                    // Ignore stopping state
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });
    }

    public void Attach(MainWindow view)
    {
        View = view;

        var context = new AttachContext
        {
            InsertAppActionGroup = View.InsertActionGroup,
            SetAccelsForAction = _application.SetAccelsForAction
        };

        _welcomePagePresenter.Attach(View.WelcomePage, context);
        _mainPagePresenter.Attach(View.MainPage, context);

        InitializeActions(context);

        View.TogglePage(MainWindow.MainPages.Welcome);
    }

    public async Task StartupAsync()
    {
        await _ariaControl.InitializeAsync();
        View.Show();

        var autoConnected = await _welcomePagePresenter.TryStartAutoConnectAsync();
        if (!autoConnected)
        {
            await _welcomePagePresenter.RefreshAsync();
        }
    }

    public void Receive(ShowToastMessage message)
    {
        ShowToast(message.Message);
    }

    private void ShowToast(string message)
    {
        GtkDispatch.InvokeIdle(() => { View.ShowToast(message); });
    }

    [LoggerMessage(LogLevel.Critical, "Failed to disconnect.")]
    partial void LogFailedToDisconnect(Exception e);

    
}