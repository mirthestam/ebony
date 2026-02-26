using Aria.Backends.MPD.Connection;
using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using Aria.Infrastructure;
using Aria.Infrastructure.Extraction;
using Aria.Infrastructure.Inspection;
using Microsoft.Extensions.Logging;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;
using FindCommand = Aria.Backends.MPD.Connection.Commands.Find.FindCommand;

namespace Aria.Backends.MPD;

public partial class Library
{
    // Inspection
    public override async Task InspectLibraryAsync(CancellationToken ct = default)
    {
        using var scope = await client.CreateConnectionScopeAsync(token: ct);

        LogStartingLibraryInspection(logger);
        
        // get tracks
        var listAllCommand = new ListAllCommand();
        var listAllResponse = await scope.SendCommandAsync(listAllCommand).ConfigureAwait(false);
        if (!listAllResponse.IsSuccess) return;
        
        foreach (var dir in listAllResponse.Content!)
        {
            ct.ThrowIfCancellationRequested();
            
            foreach (var file in dir.Files)
            {
                ct.ThrowIfCancellationRequested();
                
                var command = new FindCommand(new FilterFile(file.Path, FilterOperator.Equal));
                var response = await scope.SendCommandAsync(command).ConfigureAwait(false);
                if (!response.IsSuccess) continue;
                var tags = response.Content!.Select(x => new Tag(x.Key, x.Value)).ToList();
                
                // We do NOT have a full album here, only album info based upon this file.
                // So we cannot do inspections yet on FULL albums
                
                var inspectedAlbum = tagInspector.InspectAlbum(tags);

                foreach (var diagnostic in inspectedAlbum.Diagnostics)
                {
                    var message = $": File: {file.Path}: {diagnostic.Message}";
                    
                    switch (diagnostic.Level)
                    {
                        case Severity.Info:
                            logger.LogInformation(message);
                            break;
                        case Severity.Warning:
                            logger.LogWarning(message);
                            break;
                        case Severity.Problem:
                            logger.LogError(message);
                            break;
                    }
                }
            }
        }
        
        LogLibraryInspectionCompleted(logger);
    }

    [LoggerMessage(LogLevel.Information, "Starting library inspection. ")]
    static partial void LogStartingLibraryInspection(ILogger<Library> logger);

    [LoggerMessage(LogLevel.Information, "Library Inspection completed.")]
    static partial void LogLibraryInspectionCompleted(ILogger<Library> logger);
}

public partial class Library(Client client, ITagParser tagParser, ITagInspector tagInspector, MPDTagParser mpdTagParser, ILogger<Library> logger) : BaseLibrary
{
    public void ServerUpdated(LibraryChangedFlags flags)
    {
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