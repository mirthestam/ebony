using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Infrastructure;
using Aria.Infrastructure.Caching;
using Aria.Infrastructure.Inspection;
using Microsoft.Extensions.Logging;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;
using FindCommand = Aria.Backends.MPD.Connection.Commands.Find.FindCommand;

namespace Aria.Backends.MPD;

public partial class Library(Client client, ITagParser tagParser, ITagInspector tagInspector, MPDTagParser mpdTagParser, ILibraryCache cache, IAlbumArtCache albumArtCache, ILogger<Library> logger) : BaseLibrary(albumArtCache, logger)
{
    public void ServerUpdated(LibraryChangedFlags flags)
    {
        if (flags.HasFlag(LibraryChangedFlags.Artists))
        {
            cache.Invalidate("artists:");
        }

        if (flags.HasFlag(LibraryChangedFlags.Albums))
        {
            cache.Invalidate("albums:");
            cache.Invalidate("album:");
        }

        if (flags.HasFlag(LibraryChangedFlags.Tracks))
        {
            cache.Invalidate("track:");
        }
        
        OnUpdated(flags);
    }

    public override async Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default)
    {
        return id switch
        {
            PlaylistId playlist => await GetPlaylistAsync(playlist, cancellationToken).ConfigureAwait(false),
            TrackId track => await GetAlbumTrackAsync(track, cancellationToken).ConfigureAwait(false),
            AlbumId album => await GetAlbumAsync(album, cancellationToken).ConfigureAwait(false),
            ArtistId artist => await GetArtistAsync(artist, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException()
        };
    }

    public override async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (query.Length < 3) return SearchResults.Empty;

        var results = new SearchResults();

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        results = await AppendFindAsync(MpdTags.Album, scope, results).ConfigureAwait(false);
        results = await AppendFindAsync(MpdTags.Artist, scope, results).ConfigureAwait(false);
        results = await AppendFindAsync(MpdTags.AlbumArtist, scope, results).ConfigureAwait(false);
        results = await AppendFindAsync(MpdTags.Title, scope, results).ConfigureAwait(false);

        return results;

        async Task<SearchResults> AppendFindAsync(ITag tag, ConnectionScope innerScope, SearchResults existingResults)
        {
            var filter = new List<IFilter> { new FilterTag(tag, query, FilterOperator.Contains) };
            var command = new FindCommand(filter);
            var response = await innerScope.SendCommandAsync(command).ConfigureAwait(false);
            return AppendResults(response, ref existingResults);
        }

        SearchResults AppendResults(CommandResult<IEnumerable<KeyValuePair<string, string>>> result, ref SearchResults existingResults)
        {
            if (!result.IsSuccess) return existingResults;
            
            var tags = result.Content!.Select(x => new Tag(x.Key, x.Value)).ToList();
            var albums = _mpdTagParser.ParseAlbums(tags).ToList();

            var foundAlbums = new List<AlbumInfo>();
            var artists = new List<ArtistInfo>();
            var foundTracks = new List<TrackInfo>();
            
            foreach (var album in albums)
            {

                if (album.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    foundAlbums.Add(album);
                }
                
                foreach (var creditArtist in album.CreditsInfo.Artists.Where(a => a.Artist.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                {
                    AddArtist(artists, creditArtist.Artist, creditArtist.Roles);
                }

                foreach (var albumArtist in album.CreditsInfo.AlbumArtists.Where(a =>
                             a.Artist.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                {
                    AddArtist(artists, albumArtist.Artist, albumArtist.Roles);
                }

                foreach (var track in album.Tracks)
                {
                    if (track.Track.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        foundTracks.Add(track.Track);
                    }
                }
            }

            existingResults = existingResults with
            {
                Albums = existingResults.Albums.Concat(foundAlbums).DistinctBy(a => a.Id).ToList(), 
                Artists = existingResults.Artists.Concat(artists).DistinctBy(a => a.Id).ToList(),
                Tracks = existingResults.Tracks.Concat(foundTracks).DistinctBy(a => a.Id).ToList()
            };

            return existingResults;
        }

        void AddArtist(List<ArtistInfo> artists, ArtistInfo creditArtist, ArtistRoles roles)
        {
            var existingArtist = artists.FirstOrDefault(a => a.Id == creditArtist.Id);
            if (existingArtist != null)
            {
                artists.Remove(existingArtist);
                artists.Add(existingArtist with
                {
                    Roles = existingArtist.Roles | roles
                });
            }
            else
            {
                artists.Add(creditArtist);
            }
        }
    }

    public override async Task BeginRefreshAsync()
    {
        var command = new UpdateCommand();
        await client.SendCommandAsync(command).ConfigureAwait(false);
    }
}