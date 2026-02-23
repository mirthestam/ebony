using Adw;
using Aria.Features.Browser.Album;
using Aria.Features.Browser.Albums;
using Aria.Features.Browser.Artist;
using Aria.Features.Browser.Artists;
using Aria.Features.Browser.Playlists;
using Aria.Features.Browser.Search;
using GObject;
using Gtk;

namespace Aria.Features.Browser;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(BrowserPage)}.ui")]
public partial class BrowserPage
{
    private const string SearchPageName = "search-nav-page";
    [Connect("browser-nav-view")] private NavigationView _browserNavigationView;
    [Connect("library-albums")] private AlbumsPage _libraryAlbumsPage;
    [Connect("library-playlists")] private PlaylistsPage _libraryPlaylistsPage;
    [Connect("library-artist-detail")] private ArtistPage _libraryArtistPage;
    [Connect("library-artist-list")] private ArtistsPage _libraryArtistsPage;

    [Connect("library-nav-split-view")] private NavigationSplitView _libraryNavigationSplitView;

    [Connect("library-nav-view")] private NavigationView _libraryNavigationView;
    [Connect("search-page")] private SearchPage _searchPage;

    public ArtistPage LibraryArtistPage => _libraryArtistPage;
    public ArtistsPage LibraryArtistsPage => _libraryArtistsPage;
    public AlbumsPage LibraryAlbumsPage => _libraryAlbumsPage;
    public SearchPage SearchPage => _searchPage;
    public NavigationSplitView NavigationSplitView => _libraryNavigationSplitView;
    public PlaylistsPage LibraryPlaylistsPage => _libraryPlaylistsPage;

    partial void Initialize()
    {
        _libraryNavigationView.OnPopped += LibraryNavigationViewOnOnPopped;
    }

    private void LibraryNavigationViewOnOnPopped(NavigationView sender, NavigationView.PoppedSignalArgs args)
    {
        if (args.Page is AlbumPage albumPage)
        {
            albumPage.Dispose();
        }
    }


    public void StartSearch()
    {
        if (_browserNavigationView.VisiblePageTag == SearchPageName) return;

        _browserNavigationView.PushByTag(SearchPageName);
    }

    public void ShowArtistDetailRoot()
    {
        _browserNavigationView.Pop();
        _libraryNavigationView.ReplaceWithTags(["library-artist-detail"]);
        _libraryNavigationSplitView.SetShowContent(true);        
    }

    public void ShowAllAlbumsRoot()
    {
        // Replace the stack with the albums navigation 'tree'
        _libraryArtistsPage.Unselect();
        _browserNavigationView.Pop();
        _libraryNavigationView.ReplaceWithTags(["library-albums"]);
        _libraryNavigationSplitView.SetShowContent(true);
    }
    
    public void ShowPlaylists()
    {
        _libraryArtistsPage.Unselect();
        _browserNavigationView.Pop();
        _libraryNavigationView.ReplaceWithTags(["library-playlists"]);
        _libraryNavigationSplitView.SetShowContent(true);        
    }    
    
    public AlbumPage PushAlbumPage()
    {
        var page = AlbumPage.NewWithProperties([]);
        _browserNavigationView.Pop();        
        _libraryNavigationView.Push(page);
        _libraryNavigationSplitView.SetShowContent(true);
        return page;
        
        
    }
}