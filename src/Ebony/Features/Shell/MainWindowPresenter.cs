using Ebony.Core;
using Ebony.Core.Connection;
using Ebony.Core.Library;
using Ebony.Core.Queue;
using Ebony.Features.Shell.Welcome;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Application = Adw.Application;
using Task = System.Threading.Tasks.Task;

namespace Ebony.Features.Shell;

public partial class MainWindowPresenter : IRecipient<ShowToastMessage>
{
    private readonly Application _application;
    private readonly ILogger<MainWindowPresenter> _logger;
    private readonly IEbony _ebony;
    private readonly IEbonyControl _ebonyControl;
    private readonly MainPagePresenter _mainPagePresenter;
    private readonly WelcomePagePresenter _welcomePagePresenter;
    private readonly ArtAssetLoader _artAssetLoader;

    public MainWindow View { get; private set; }

    public MainWindowPresenter(IMessenger messenger,
        MainPagePresenter mainPagePresenter,
        WelcomePagePresenter welcomePagePresenter,
        ILogger<MainWindowPresenter> logger,
        Application application,
        IEbony Ebony,
        IEbonyControl ebonyControl, ArtAssetLoader artAssetLoader)
    {
        _application = application;
        _ebony = Ebony;
        _mainPagePresenter = mainPagePresenter;
        _welcomePagePresenter = welcomePagePresenter;
        _ebonyControl = ebonyControl;
        _artAssetLoader = artAssetLoader;
        _logger = logger;

        messenger.RegisterAll(this);

        _ebonyControl.StateChanged += EbonyControlOnStateChanged;
    }

    private async void EbonyControlOnStateChanged(object? sender, EngineStateChangedEventArgs e)
    {
        try
        {
            await GtkDispatch.InvokeIdleAsync(() =>
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
        catch (Exception)
        {
            // TODO: Log
        }
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
        await _ebonyControl.InitializeAsync();
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

    private async void ShowToast(string message)
    {
        try
        {
            await GtkDispatch.InvokeIdleAsync(() => { View.ShowToast(message); });
        }
        catch (Exception)
        {
            // TODO: Log
        }
    }

    [LoggerMessage(LogLevel.Critical, "Failed to disconnect.")]
    partial void LogFailedToDisconnect(Exception e);

    
}