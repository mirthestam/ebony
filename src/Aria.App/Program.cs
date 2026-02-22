using Aria.App.Infrastructure;
using Aria.Backends.MPD;
using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.UI;
using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Features.Browser;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Playlists;
using Aria.Features.Browser.Search;
using Aria.Features.Details;
using Aria.Features.Player;
using Aria.Features.Player.Queue;
using Aria.Features.PlayerBar;
using Aria.Features.Shared;
using Aria.Features.Shell;
using Aria.Features.Shell.Welcome;
using Aria.Hosting;
using Aria.Hosting.Extensions;
using Aria.Infrastructure;
using Aria.Infrastructure.Extraction;
using Aria.Infrastructure.Inspection;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MainWindow = Aria.Features.Shell.MainWindow;
using Task = System.Threading.Tasks.Task;

namespace Aria.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = CreateHostBuilder(args);
        var host = builder.Build();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.SingleLine = true;
                    options.IncludeScopes = false;                    
                });
            })
            .ConfigureServices(x =>
            {
                // Messaging
                x.AddSingleton<IMessenger>(_ => WeakReferenceMessenger.Default);

                // Infrastructure
                x.AddSingleton<DiskConnectionProfileSource>();
                x.AddSingleton<AriaEngine>();
                x.AddSingleton<IAriaControl>(sp => sp.GetRequiredService<AriaEngine>());
                x.AddSingleton<IAria>(sp => sp.GetRequiredService<AriaEngine>());
                x.AddSingleton<ILibrary>(sp => sp.GetRequiredService<IAria>().Library);
                
                x.AddSingleton<IConnectionProfileProvider, ConnectionProfileProvider>();
                x.AddSingleton<ArtAssetLoader>();
                x.AddTransient<ITagParser, PicardTagParser>();
                x.AddTransient<ITagInspector, PicardTagInspector>();
                x.AddSingleton<IPresenterFactory, PresenterFactory>();
                x.AddSingleton<IPlaylistNameValidator, PlaylistNameValidator>();
                
                x.AddSingleton<MainWindowPresenter>();
                x.AddSingleton<MainPagePresenter>();
                x.AddSingleton<WelcomePagePresenter>();

                // Features - Browser
                x.AddSingleton<BrowserHostPresenter>();
                x.AddSingleton<BrowserPagePresenter>();
                
                x.AddSingleton<AlbumPagePresenter>();
                x.AddSingleton<AlbumsPagePresenter>();                
                x.AddSingleton<ArtistPagePresenter>();
                x.AddSingleton<ArtistsPagePresenter>();
                x.AddSingleton<PlaylistsPagePresenter>();                
                x.AddSingleton<SearchPagePresenter>();

                // Features - Details
                x.AddSingleton<TrackDetailsDialogPresenter>();
                
                // Features - Player
                x.AddSingleton<PlayerPresenter>();
                x.AddSingleton<QueuePresenter>();

                // Feature - PlayerBar
                x.AddSingleton<PlayerBarPresenter>();
                
                // MPD
                x.AddSingleton<IBackendConnectionFactory, BackendConnectionFactory>();
                x.AddSingleton<IConnectionProfileFactory, ConnectionProfileFactory>();
                x.AddSingleton<IConnectDialogPresenter, Backends.MPD.UI.Connect.ConnectDialogPresenter>();                
                x.AddTransient<BackendConnection>();
                x.AddScoped<Backends.MPD.Queue>();
                x.AddScoped<Library>();
                x.AddScoped<Client>();
                x.AddScoped<Backends.MPD.Player>();
                x.AddScoped<Backends.MPD.Extraction.MPDTagParser>();
                x.AddScoped<IIdProvider, Backends.MPD.Extraction.IdProvider>();
            })
            
            .UseGtk(a =>
            {
                a.GtkApplicationType = GtkApplicationType.Adw;
                a.ApplicationId = "nl.mirthestam.aria";
                a.ApplicationFlags = ApplicationFlags.FlagsNone;

                a.UseWindow<MainWindow>(provider =>
                {
                    var presenter = provider.GetRequiredService<MainWindowPresenter>();
                    var logger = provider.GetRequiredService<ILogger<MainWindow>>();
                    return MainWindow.New(presenter, logger);
                });

                a.WithResource("nl.mirthestam.aria.gresource");

                // GObject registrations (GType)
                a.WithMainGTypes();
                a.WithBrowserGTypes();
                a.WithPlayerGTypes();
                a.WithPlayerBarGTypes();
                a.WithSharedGTypes();
                a.WithMPDGTypes();
                a.WithDetailsGTypes();
            });
    }
}