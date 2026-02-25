using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;
using GdkPixbuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aria.Infrastructure.Caching;

public sealed partial class ResourceCacheLibrarySource : ILibrarySource
{
    private readonly ILibrarySource _innerLibrary;
    private readonly string _cacheDir;
    private readonly TimeSpan? _ttl;
    private readonly ILogger<ResourceCacheLibrarySource> _logger;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public ResourceCacheLibrarySource(
        ILibrarySource innerLibrary,
        string connectionScopeKey,
        TimeSpan? ttl = null)
    {
        // TODO: Inject a real logger here
        _logger = NullLogger<ResourceCacheLibrarySource>.Instance;

        _innerLibrary = innerLibrary ?? throw new ArgumentNullException(nameof(innerLibrary));
        _ttl = ttl;

        var baseCacheDir =
            Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");

        _cacheDir = Path.Combine(baseCacheDir, "aria", "album-res", Sanitize(connectionScopeKey));
        Directory.CreateDirectory(_cacheDir);

        LogCacheInitialized(_cacheDir, _ttl?.ToString() ?? "<none>");

        // Best-effort cleanup of stale tmp files from previous crashes
        TryCleanupTmpFiles();
    }

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(CancellationToken cancellationToken = default)
        => _innerLibrary.GetArtistsAsync(cancellationToken);

    public Task<IEnumerable<ArtistInfo>> GetArtistsAsync(ArtistQuery query,
        CancellationToken cancellationToken = default) => _innerLibrary.GetArtistsAsync(query, cancellationToken);

    public Task<ArtistInfo?> GetArtistAsync(Id artistId, CancellationToken cancellationToken = default)
        => _innerLibrary.GetArtistAsync(artistId, cancellationToken);

    public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(CancellationToken cancellationToken = default)
        => _innerLibrary.GetAlbumsAsync(cancellationToken);

    public Task<IEnumerable<AlbumInfo>> GetAlbumsAsync(Id artistId, CancellationToken cancellationToken = default)
        => _innerLibrary.GetAlbumsAsync(artistId, cancellationToken);

    public Task<AlbumInfo?> GetAlbumAsync(Id albumId, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetAlbumAsync(albumId, cancellationToken);

    public Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        _innerLibrary.SearchAsync(query, cancellationToken);

    public Task<IEnumerable<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        return _innerLibrary.GetPlaylistsAsync(cancellationToken);
    }

    public Task<PlaylistInfo?> GetPlaylistAsync(Id playlistId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Info?> GetItemAsync(Id id, CancellationToken cancellationToken = default) =>
        _innerLibrary.GetItemAsync(id, cancellationToken);

    public Task DeletePlaylistAsync(Id id, CancellationToken cancellationToken = default)
    {
        return _innerLibrary.DeletePlaylistAsync(id, cancellationToken);
    }

    public Task RenamePlaylistAsync(Id id, string newName, CancellationToken cancellationToken = default)
    {
        return _innerLibrary.RenamePlaylistAsync(id, newName, cancellationToken);
    }

    public Task BeginRefreshAsync()
    {
        return _innerLibrary.BeginRefreshAsync();
    }

    public async Task<Stream> GetAlbumResourceStreamAsync(Id resourceId, CancellationToken cancellationToken = default)
    {
        var key = resourceId.ToString();
        var fileName = Path.Combine(_cacheDir, Sha256Hex(key) + ".bin");

        if (TryOpenIfValid(fileName, out var cachedStream))
        {
            LogCacheHit(resourceId, fileName);
            return cachedStream;
        }

        LogCacheMiss(resourceId);

        var gate = _gates.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (TryOpenIfValid(fileName, out cachedStream))
            {
                LogCacheHit(resourceId, fileName);
                return cachedStream;
            }

            await using var stream =
                await _innerLibrary.GetAlbumResourceStreamAsync(resourceId, cancellationToken).ConfigureAwait(false);

            var tmp = fileName + ".tmp";
            long bytesWritten;

            await using (var fs = new FileStream(
                             tmp,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 64 * 1024,
                             useAsync: true))
            {
                await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                bytesWritten = fs.Length;
            }

            try
            {
                TryDelete(fileName);
                if (!TryCreateThumbnailPng(await File.ReadAllBytesAsync(tmp, cancellationToken), 256, 256, fileName))
                {
                    throw new Exception("Failed to create thumbnail");
                }

                LogCacheWriteSuccess(resourceId, bytesWritten, fileName);
            }
            catch (Exception e)
            {
                TryDelete(tmp);
                LogCacheWriteFailed(e, resourceId, fileName);
                throw;
            }
            finally
            {
                TryDelete(tmp);                
            }

            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024,
                useAsync: true);
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool TryCreateThumbnailPng(byte[] inputBytes, int maxWidth, int maxHeight, string fileName)
    {
        try
        {
            using var loader = PixbufLoader.NewWithProperties([]);
                
            loader.Write(inputBytes);
            loader.Close();
            
            var pixelBuffer = loader.GetPixbuf();
            if (pixelBuffer == null) return false;
            
            if (pixelBuffer.Width <= 0 || pixelBuffer.Height <= 0) return false;
            
            var scaledPixelBuffer = pixelBuffer.ScaleSimple(maxWidth, maxHeight, InterpType.Bilinear);
            return scaledPixelBuffer != null && scaledPixelBuffer.Savev(fileName, "png", [], []);
        }
        catch
        {
            return false;
        }
    }    
    
    private bool TryOpenIfValid(string fileName, out Stream stream)
    {
        stream = Stream.Null;

        try
        {
            if (!File.Exists(fileName))
                return false;

            if (_ttl is not null)
            {
                var createdUtc = File.GetLastWriteTimeUtc(fileName);
                if (DateTime.UtcNow - createdUtc > _ttl.Value)
                {
                    LogCacheExpired(fileName);
                    TryDelete(fileName);
                    return false;
                }
            }

            stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024,
                useAsync: true);
            return true;
        }
        catch (Exception e)
        {
            LogCacheReadFailed(e, fileName);
            TryDelete(fileName);
            stream = Stream.Null;
            return false;
        }
    }

    private void TryCleanupTmpFiles()
    {
        try
        {
            var deleted = 0;
            foreach (var tmp in Directory.EnumerateFiles(_cacheDir, "*.tmp", SearchOption.TopDirectoryOnly))
            {
                if (TryDelete(tmp))
                    deleted++;
            }

            if (deleted > 0)
                LogTmpCleanup(deleted);
        }
        catch (Exception e)
        {
            LogTmpCleanupFailed(e);
        }
    }

    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "default";

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_');

        return sb.ToString();
    }

    private bool TryDelete(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            LogDeleteFailed(e, path);
            return false;
        }
    }

    public event EventHandler<LibraryChangedEventArgs>? Updated
    {
        add => _innerLibrary.Updated += value;
        remove => _innerLibrary.Updated -= value;
    }

    public Task InspectLibraryAsync(CancellationToken ct = default)
    {
        return _innerLibrary.InspectLibraryAsync(ct);
    }

    [LoggerMessage(LogLevel.Information, "Album resource cache initialized at '{cacheDir}', TTL={ttl}")]
    private partial void LogCacheInitialized(string cacheDir, string ttl);

    [LoggerMessage(LogLevel.Debug, "Album resource cache HIT for {resourceId} ({filePath})")]
    private partial void LogCacheHit(Id resourceId, string filePath);

    [LoggerMessage(LogLevel.Debug, "Album resource cache MISS for {resourceId}")]
    private partial void LogCacheMiss(Id resourceId);

    [LoggerMessage(LogLevel.Debug, "Album resource cache expired: {filePath}")]
    private partial void LogCacheExpired(string filePath);

    [LoggerMessage(LogLevel.Warning, "Album resource cache read failed for {filePath}")]
    private partial void LogCacheReadFailed(Exception e, string filePath);

    [LoggerMessage(LogLevel.Debug, "Album resource cached for {resourceId}: {bytes} bytes -> {filePath}")]
    private partial void LogCacheWriteSuccess(Id resourceId, long bytes, string filePath);

    [LoggerMessage(LogLevel.Warning, "Album resource cache write failed for {resourceId} -> {filePath}")]
    private partial void LogCacheWriteFailed(Exception e, Id resourceId, string filePath);

    [LoggerMessage(LogLevel.Debug, "Album resource cache tmp cleanup removed {count} files")]
    private partial void LogTmpCleanup(int count);

    [LoggerMessage(LogLevel.Warning, "Album resource cache tmp cleanup failed")]
    private partial void LogTmpCleanupFailed(Exception e);

    [LoggerMessage(LogLevel.Warning, "Album resource cache failed to delete '{filePath}'")]
    private partial void LogDeleteFailed(Exception e, string filePath);
}