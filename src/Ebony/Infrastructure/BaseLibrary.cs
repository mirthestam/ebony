using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace Ebony.Infrastructure;

public abstract class BaseLibrary(IAlbumArtCache albumArtCache, ILogger? logger = null) : ILibrarySource
{
    private ILogger? Logger { get; } = logger;
    
    public virtual event EventHandler<LibraryChangedEventArgs>? Updated;
    public abstract Task InspectLibraryAsync(CancellationToken ct = default);
    public abstract Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default);
    public abstract Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default);
    
    public abstract Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query, CancellationToken cancellationToken = default);
    public abstract Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default);

    public abstract Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default);
    public abstract Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default);    
    
    public abstract Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);

    public abstract Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default);

    public abstract Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default);

    public abstract Task RenamePlaylistAsync(Id id, string newName, CancellationToken cancellationToken = default);

    public virtual Task BeginRefreshAsync() => Task.CompletedTask;

    public async Task<Stream> GetAlbumResourceStreamAsync(Id assetId, CancellationToken ct)
    {
        if (assetId == Id.Empty)
        {
            return await GetDefaultAlbumResourceStreamAsync(ct).ConfigureAwait(false);
        }

        try
        {
            // Check if we already know this item (either cached or previously failed)
            if (!albumArtCache.Contains(assetId))
            {
                // Try to retrieve this item from the backend
                var backendStream = await GetAlbumResourceFromBackend(assetId, ct);
                if (backendStream == null)
                {
                    // The cache will remember the failed attempt.
                    // This way, we make sure we won't retry every restart of the application
                    albumArtCache.MarkFailed(assetId);
                    return await GetDefaultAlbumResourceStreamAsync(ct).ConfigureAwait(false);
                }

                // Save it to the cache
                var cacheStream = await albumArtCache.CreateThumbnailAndCacheStream(assetId, backendStream, ct);
                if (cacheStream != null) return cacheStream;
            }
            
            // Try cache (might have been populated by another thread)
            var cachedStream = albumArtCache.GetAlbumResourceStream(assetId);
            if (cachedStream != null) return cachedStream;
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to get album art for {Id}", assetId);
        }
        
        return await GetDefaultAlbumResourceStreamAsync(ct).ConfigureAwait(false);
    }

    protected abstract Task<Stream?> GetAlbumResourceFromBackend(Id id, CancellationToken ct);

    private static async Task<Stream> GetDefaultAlbumResourceStreamAsync(CancellationToken ct = default)
    {
        var assembly = typeof(BaseLibrary).Assembly;

        await using var stream = assembly.GetManifestResourceStream("Ebony.Resources.vinyl-record.png");

        if (stream == null)
        {
            return Stream.Null;
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var buffer = ms.ToArray();

        return new MemoryStream(buffer);
    }
    
    protected void OnUpdated(LibraryChangedFlags flags)
    {
        Updated?.Invoke(this, new LibraryChangedEventArgs(flags));
    }            
}