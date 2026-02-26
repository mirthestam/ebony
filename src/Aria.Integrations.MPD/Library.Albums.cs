using Aria.Backends.MPD.Extraction;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Microsoft.Extensions.Logging;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;
using FindCommand = Aria.Backends.MPD.Connection.Commands.Find.FindCommand;
using SearchCommand = Aria.Backends.MPD.Connection.Commands.Search.SearchCommand;

namespace Aria.Backends.MPD;

public partial class Library
{
    private readonly MPDTagParser _mpdTagParser = new(tagParser);
    
    // Albums
    public override async Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default)
    {
        var fullId = (AlbumId)albumId;

        var title = fullId.Title;
        var artistNames = fullId.AlbumArtistIds.Select(id => id).Select(id => id.Value);
        var filters = new List<KeyValuePair<ITag, string>> { new(MpdTags.Album, title) };
        filters.AddRange(artistNames.Select(name => new KeyValuePair<ITag, string>(MpdTags.AlbumArtist, name)));

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        var command = new SearchCommand(filters);
        var response = await scope.SendCommandAsync(command).ConfigureAwait(false);
        if (!response.IsSuccess) return null;

        var tags = response.Content!.Select(x => new Tag(x.Key, x.Value));
        var albums = _mpdTagParser.ParseAlbums(tags.ToList()).ToList();

        switch (albums.Count)
        {
            case 0:
            // unexpected!
            case > 1:
                // Album not found
                return null;
            default:
                return albums[0];
        }
    }
    
    public override async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        var artists = (await GetArtistsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var allTags = new List<Tag>();

        using (var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false))
        {
            var tasks = artists
                .Select(artist =>
                    scope.SendCommandAsync(new SearchCommand(MpdTags.AlbumArtist, artist.Name)))
                .ToList();

            var responses = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var response in responses)
            {
                if (response is { IsSuccess: true, Content: not null })
                {
                    allTags.AddRange(response.Content.Select(x => new Tag(x.Key, x.Value)));
                }
            }
        }

        // This is CPU-bound parsing. Since ConfigureAwait(false) was used earlier,
        // this code is assumed to be running on a background thread.
        return _mpdTagParser.ParseAlbums(allTags);
    }
    
    public override async Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId,
        CancellationToken cancellationToken = default)
    {
        var mpdArtistId = (ArtistId)artistId;
        if (_artistAliases.IsEmpty) throw new InvalidOperationException("Artists has not been initialized yet");
        
        // Expand to all known backend aliases for this canonical artist
        var aliasMap = _artistAliases.GetValueOrDefault(mpdArtistId);
        var namesToQuery = (aliasMap?.Keys ?? Enumerable.Empty<string>())
            .Append(mpdArtistId.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();        

        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);

        // Either FindCommand or SearchCommand could be used here. SearchCommand is faster because it does not support expressions.
        var tasks = namesToQuery.SelectMany(name => new[]
        {
            scope.SendCommandAsync(new SearchCommand(MpdTags.AlbumArtist, name)),
            scope.SendCommandAsync(new SearchCommand(MpdTags.Artist, name)),
            scope.SendCommandAsync(new SearchCommand(MpdTags.Composer, name)),
            scope.SendCommandAsync(new SearchCommand(ExtraMpdTags.Conductor, name)),
            scope.SendCommandAsync(new SearchCommand(ExtraMpdTags.Ensemble, name)),
            scope.SendCommandAsync(new SearchCommand(MpdTags.Performer, name))
        }).ToArray();

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var allTags = results
            .Where(r => r is { IsSuccess: true, Content: not null })
            .SelectMany(r => r.Content!)
            .Select(pair => new Tag(pair.Key, pair.Value))
            .ToList();


        return _mpdTagParser.ParseAlbums(allTags);
    }
    
    private async Task<AlbumTrackInfo?> GetAlbumTrackAsync(Id trackId, CancellationToken cancellationToken)
    {
        var fullId = (TrackId)trackId;
        
        // Query the information
        using var scope = await client.CreateConnectionScopeAsync(token: cancellationToken).ConfigureAwait(false);
        var command = new FindCommand(new FilterFile(fullId.Value, FilterOperator.Equal));
        var response = await scope.SendCommandAsync(command).ConfigureAwait(false);
        if (!response.IsSuccess) return null;
        var tags = response.Content!.Select(x => new Tag(x.Key, x.Value));
        
        // Parse the information
        var albums = _mpdTagParser.ParseAlbums(tags.ToList()).ToList();

        if (albums.Count != 1) return null;
        return albums[0].Tracks.Count != 1 ? null : albums[0].Tracks.FirstOrDefault();
    }
    
    public override async Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken token)
    {
        if (resourceId == Id.Empty) return await GetDefaultAlbumResourceStreamAsync(token).ConfigureAwait(false);

        var albumArtId = (AssetId)resourceId;
        var fileName = albumArtId.Value;

        using var scope = await client.CreateConnectionScopeAsync(token: token).ConfigureAwait(false);


        // Try to find the cover from the directory the track resides in by looking for a file called cover.png, cover.jpg, or cover.webp.
        try
        {
            long totalBinarySize;
            long currentSize = 0;
            var data = new List<byte>();

            do
            {
                var (isSuccess, content) = await scope.SendCommandAsync(new AlbumArtCommand(fileName, currentSize))
                    .ConfigureAwait(false);
                if (!isSuccess) break;
                if (content == null) break;

                if (content.Binary == 0) break;

                totalBinarySize = content.Size;
                currentSize += content.Binary;
                data.AddRange(content.Data);
            } while (currentSize < totalBinarySize && !token.IsCancellationRequested);

            if (data.Count > 0)
            {
                return new MemoryStream(data.ToArray());
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            LogFailedToGetAlbumArtFromMPD(logger, e);
        }

        // Try to find the art by reading embedded pictures from binary tags (e.g., ID3v2’s APIC tag).
        try
        {
            long totalBinarySize;
            long currentSize = 0;
            var data = new List<byte>();

            do
            {
                var (isSuccess, content) = await scope.SendCommandAsync(new ReadPictureCommand(fileName, currentSize));
                if (!isSuccess) break;
                if (content == null) break;
                if (content.Binary == 0) break;

                totalBinarySize = content.Size;
                currentSize += content.Binary;
                data.AddRange(content.Data);
            } while (currentSize < totalBinarySize && !token.IsCancellationRequested);

            if (data.Count > 0)
            {
                return new MemoryStream(data.ToArray());
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            LogFailedToGetAlbumArtFromMPD(logger, e);
        }

        // No album art found. Just return the default album art.
        return await GetDefaultAlbumResourceStreamAsync(token);
    }

    [LoggerMessage(LogLevel.Error, "Failed to get album art from MPD")]
    static partial void LogFailedToGetAlbumArtFromMPD(ILogger<Library> logger, Exception e);
}