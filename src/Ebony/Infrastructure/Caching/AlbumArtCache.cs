using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Ebony.Core.Connection;
using Ebony.Core.Extraction;
using Microsoft.Extensions.Logging;

namespace Ebony.Infrastructure.Caching;

public sealed partial class AlbumArtCache : IAlbumArtCache
{
    private const string FailedSuffix = ".failed";
    
    private readonly string _cacheDir;
    
    private readonly TimeSpan? _ttl = TimeSpan.FromDays(30);
    private readonly IThumbnailTool _thumbnailTool;
    private readonly ILogger<AlbumArtCache> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    
    public AlbumArtCache(ConnectionContext connectionContext, IThumbnailTool thumbnailTool, ILogger<AlbumArtCache> logger)
    {
        _thumbnailTool = thumbnailTool;
        _logger = logger;

        var baseCacheDir =
            Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");

        _cacheDir = Path.Combine(baseCacheDir, "Ebony", Sanitize(connectionContext.Profile.Id.ToString()), "album-art");
        Directory.CreateDirectory(_cacheDir);
        
        TryCleanupTmpFiles();
    }
    
    public Stream? GetAlbumResourceStream(Id resourceId)
    {
        var fileName = ComposeFilename(resourceId);
        
        if (TryOpenIfValid(fileName, out var cachedStream))
        {
            LogCacheHit(resourceId, fileName);
            return cachedStream;
        }

        LogCacheMiss(resourceId);

        return null;
    }
    public bool Contains(Id resourceId)
    {
        var fileName = ComposeFilename(resourceId);
        
        // Known if either the cached file exists OR the failure marker exists
        return File.Exists(fileName) || File.Exists(fileName + FailedSuffix);
    }

    public void MarkFailed(Id resourceId)
    {
        var fileName = ComposeFilename(resourceId);
        var failedFile = fileName + FailedSuffix;
        
        try
        {
            File.WriteAllText(failedFile, DateTime.UtcNow.ToString("O"));
        }
        catch (Exception e)
        {
            LogDeleteFailed(e, failedFile);
        }
    }

    private string ComposeFilename(Id resourceId)
    {
        var key = resourceId.ToString();
        return Path.Combine(_cacheDir, Sha256Hex(key) + ".bin");        
    }
    
    public async Task<Stream?> CreateThumbnailAndCacheStream(Id resourceId, Stream sourceStream,  CancellationToken cancellationToken = default)
    {
        var fileName = ComposeFilename(resourceId);

        // Get or create a lock for this specific resource
        var gate = _locks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if another thread already cached it while we were waiting
            if (TryOpenIfValid(fileName, out var existingStream))
            {
                LogCacheHit(resourceId, fileName);
                return existingStream;
            }

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
                await sourceStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                bytesWritten = fs.Length;
            }

            try
            {
                TryDelete(fileName);
                if (!_thumbnailTool.TryCreateThumbnailPng(await File.ReadAllBytesAsync(tmp, cancellationToken), 256, 256, fileName))
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

            return GetAlbumResourceStream(resourceId);
        }
        finally
        {
            gate.Release();

            // Cleanup: remove lock if no one is waiting
            if (gate.CurrentCount == 1)
            {
                _locks.TryRemove(fileName, out _);
            }
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