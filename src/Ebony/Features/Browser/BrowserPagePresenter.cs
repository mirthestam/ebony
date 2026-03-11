using Ebony.Core;
using Ebony.Core.Extraction;
using Ebony.Features.Browser.Album;
using Ebony.Features.Browser.Albums;
using Ebony.Features.Browser.Artist;
using Ebony.Features.Browser.Artists;
using Ebony.Features.Browser.Playlists;
using Ebony.Features.Browser.Search;
using Ebony.Features.Shell;
using Ebony.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Ebony.Features.Browser;

public partial class BrowserPagePresenter : IRootPresenter<BrowserPage>
{
    private readonly IEbony _ebony;
    private readonly IEbonyControl _ebonyControl;
    private readonly IMessenger _messenger;
    private readonly IPresenterFactory _presenterFactory;
    private readonly AlbumsPagePresenter _albumsPagePresenter;

    private readonly ArtistPagePresenter _artistPagePresenter;
    private readonly ArtistsPagePresenter _artistsPagePresenter;
    private readonly PlaylistsPagePresenter _playlistsPagePresenter;
    
    private readonly ILogger<BrowserPage> _logger;
    private readonly SearchPagePresenter _searchPagePresenter;

    private AlbumPagePresenter? _albumPagePresenter;
    
    public BrowserPagePresenter(ILogger<BrowserPage> logger,
        IMessenger messenger,
        IEbony Ebony,
        IEbonyControl ebonyControl,
        AlbumsPagePresenter albumsPagePresenter,
        ArtistPagePresenter artistPagePresenter,
        ArtistsPagePresenter artistsPagePresenter,
        IPresenterFactory presenterFactory,
        SearchPagePresenter searchPagePresenter, 
        PlaylistsPagePresenter playlistsPagePresenter)
    {
        _logger = logger;
        _messenger = messenger;
        _ebony = Ebony;
        _ebonyControl = ebonyControl;
        _artistPagePresenter = artistPagePresenter;
        _artistsPagePresenter = artistsPagePresenter;
        _searchPagePresenter = searchPagePresenter;
        _playlistsPagePresenter = playlistsPagePresenter;
        _albumsPagePresenter = albumsPagePresenter;
        _presenterFactory = presenterFactory;
    }

    public BrowserPage? View { get; private set; }

    public void Attach(BrowserPage view, AttachContext context)
    {
        View = view;
        _artistPagePresenter.Attach(view.LibraryArtistPage);
        _artistsPagePresenter.Attach(view.LibraryArtistsPage);
        _albumsPagePresenter.Attach(view.LibraryAlbumsPage);
        _searchPagePresenter.Attach(view.SearchPage);
        _playlistsPagePresenter.Attach(view.LibraryPlaylistsPage, context);
        
        InitializeActions(context);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        LogRefreshingBrowserPage();

        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.ShowAllAlbumsRoot();
        }, cancellationToken).ConfigureAwait(false);        
        
        // Preload the artists
        await _artistsPagePresenter.RefreshAsync(cancellationToken);
        
        // Preload the playlists
        await _playlistsPagePresenter.RefreshAsync(cancellationToken);
        
        // Load all albums 
        await _albumsPagePresenter.RefreshAsync(cancellationToken).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            if (t.Exception is not null) LogFailedToLoadLibrary(t.Exception);
        }, TaskScheduler.Default);

        if (cancellationToken.IsCancellationRequested)
            LogBrowserPageRefreshCancelled();
        else
            LogBrowserPageRefreshed();
    }

    public async Task ResetAsync()
    {
        try
        {
            LogResettingBrowserPage();
            _albumPagePresenter?.Reset();
            await _playlistsPagePresenter.ResetAsync();
            _albumsPagePresenter.Reset();
            _artistsPagePresenter.Reset();
            await _artistPagePresenter.ResetAsync();
            _searchPagePresenter.Reset();
            View?.ShowArtistDetailRoot();
            LogBrowserPageReset();
        }
        catch (Exception e)
        {
            LogFailedToResetBrowserPage(e);
        }
    }
    
    private async Task ShowAllAlbumsAsync()
    {
        LogShowingAllAlbums();
        
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.ShowAllAlbumsRoot();
        });    
    }
    
    private async Task ShowPlaylistsAsync()
    {
        await GtkDispatch.InvokeIdleAsync(() =>
        {
            View?.ShowPlaylists();
        });    
    }    
    
    [LoggerMessage(LogLevel.Debug, "Refreshing browser page...")]
    partial void LogRefreshingBrowserPage();    

    [LoggerMessage(LogLevel.Error, "Failed to load your library")]
    partial void LogCouldNotLoadLibrary(Exception e);

    [LoggerMessage(LogLevel.Debug, "Browser page refresh cancelled.")]
    partial void LogBrowserPageRefreshCancelled();

    [LoggerMessage(LogLevel.Information, "Browser page refreshed.")]
    partial void LogBrowserPageRefreshed();

    [LoggerMessage(LogLevel.Debug, "Resetting browser page...")]
    partial void LogResettingBrowserPage();

    [LoggerMessage(LogLevel.Debug, "Browser page reset.")]
    partial void LogBrowserPageReset();

    [LoggerMessage(LogLevel.Debug, "Showing album details for {albumId}")]
    partial void LogShowingAlbumDetailsForAlbum(Id albumId);

    [LoggerMessage(LogLevel.Debug, "Showing all albums")]
    partial void LogShowingAllAlbums();

    [LoggerMessage(LogLevel.Error, "Failed to reset browser page")]
    partial void LogFailedToResetBrowserPage(Exception e);

    [LoggerMessage(LogLevel.Error, "Failed to load library")]
    partial void LogFailedToLoadLibrary(Exception e);
}