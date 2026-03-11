using Ebony.Features.Browser.Album;
using Ebony.Features.Browser.Artist;
using Ebony.Features.Browser.Artists;
using Ebony.Features.Browser.Playlists;
using Ebony.Features.Browser.Search;
using Ebony.Features.Browser.Shared;
using Ebony.Hosting;

namespace Ebony.Features.Browser;

public static class GtkBuilderExtensions
{
    extension(IGtkBuilder builder)
    {
        public void WithBrowserGTypes()
        {
            // Shared
            builder.WithGType<AlbumListItem>();
            builder.WithGType<AlbumModel>();
            builder.WithGType<AlbumsGrid>();
            
            // Album
            builder.WithGType<AlbumPage>();       
            builder.WithGType<TrackGroup>();
            builder.WithGType<CreditBox>();            
            
            // Albums
            builder.WithGType<Albums.AlbumsPage>();            
            
            // Artist
            builder.WithGType<ArtistPage>();
            builder.WithGType<EmptyPage>();
            
            // Artists
            builder.WithGType<ArtistsPage>();            
            builder.WithGType<ArtistModel>();
            builder.WithGType<ArtistListItem>();            
            
            // Playlists
            builder.WithGType<PlaylistsPage>();    
            builder.WithGType<PlaylistNameCell>();
            builder.WithGType<PlaylistsEmptyPage>();           
            builder.WithGType<RenamePlaylistDialog>();           
            
            // Search
            builder.WithGType<SearchPage>();            
            
            // Common
            builder.WithGType<BrowserPage>();            
            builder.WithGType<BrowserEmptyPage>();
            builder.WithGType<BrowserHost>();
        }
    }
}