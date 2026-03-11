using Ebony.App.Infrastructure;
using Ebony.Backends.MPD;
using Ebony.Backends.MPD.Connection;
using Ebony.Backends.MPD.UI;
using Ebony.Core;
using Ebony.Core.Connection;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Features.Browser;
using Ebony.Features.Browser.Album;
using Ebony.Features.Browser.Albums;
using Ebony.Features.Browser.Artist;
using Ebony.Features.Browser.Artists;
using Ebony.Features.Browser.Playlists;
using Ebony.Features.Browser.Search;
using Ebony.Features.Details;
using Ebony.Features.Player;
using Ebony.Features.Player.Queue;
using Ebony.Features.PlayerBar;
using Ebony.Features.Shared;
using Ebony.Features.Shell;
using Ebony.Features.Shell.Welcome;
using Ebony.Hosting;
using Ebony.Hosting.Extensions;
using Ebony.Infrastructure;
using Ebony.Infrastructure.Caching;
using Ebony.Infrastructure.Extraction;
using Ebony.Infrastructure.Inspection;
using CommunityToolkit.Mvvm.Messaging;
using Gio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MainWindow = Ebony.Features.Shell.MainWindow;
using Task = System.Threading.Tasks.Task;

namespace Ebony.App;

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
                logging.SetMinimumLevel(LogLevel.Information);
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
                x.AddSingleton<EbonyEngine>();
                x.AddSingleton<IEbonyControl>(sp => sp.GetRequiredService<EbonyEngine>());
                x.AddSingleton<IEbony>(sp => sp.GetRequiredService<EbonyEngine>());
                x.AddSingleton<ILibrary>(sp => sp.GetRequiredService<IEbony>().Library);
                x.AddScoped<ConnectionContext>();
                x.AddScoped<ILibraryCache, LibraryCache>();                
                x.AddScoped<IAlbumArtCache, AlbumArtCache>();
                x.AddScoped<IThumbnailTool, ThumbnailTool>();
                x.AddScoped<ArtAssetLoader>();                
                
                x.AddSingleton<IConnectionProfileProvider, ConnectionProfileProvider>();
                
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
                a.ApplicationId = "nl.mirthestam.ebony";
                a.ApplicationFlags = ApplicationFlags.FlagsNone;

                a.UseWindow<MainWindow>(provider =>
                {
                    var presenter = provider.GetRequiredService<MainWindowPresenter>();
                    var logger = provider.GetRequiredService<ILogger<MainWindow>>();
                    return MainWindow.New(presenter, logger);
                });

                a.WithResource("nl.mirthestam.ebony.gresource");

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