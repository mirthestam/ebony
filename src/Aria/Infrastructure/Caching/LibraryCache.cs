using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aria.Core;
using Aria.Core.Connection;
using Microsoft.Extensions.Logging;

namespace Aria.Infrastructure.Caching;

public record CollectionCacheEntry(IReadOnlyList<string> ItemKeys, DateTimeOffset CachedAt);

public class LibraryCache : ILibraryCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _memoryCache = new();
    private readonly string? _diskCachePath;
    private readonly ILogger<LibraryCache>? _logger;
    private readonly int _maxMemoryCacheItems;
    private readonly CacheJsonContext _jsonContext;
    private readonly SemaphoreSlim _diskLock = new(1, 1);
    private long _accessCounter;

    private class CacheEntry
    {
        public object Value { get; init; } = null!;
        public long LastAccessTime { get; set; }
        public DateTimeOffset CachedAt { get; init; }
    }

    public LibraryCache(ILogger<LibraryCache> logger, ConnectionContext connectionContext, IAriaControl ariaControl)
    {
        var baseCacheDir =
            Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
        
        _diskCachePath = Path.Combine(baseCacheDir, "aria", Sanitize(connectionContext.Profile.Id.ToString()), "library");
        _logger = logger;
        _maxMemoryCacheItems = 1000;
        _jsonContext = new CacheJsonContext(ariaControl);

        try
        {
            Directory.CreateDirectory(_diskCachePath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create disk cache directory: {Path}", _diskCachePath);
            _diskCachePath = null;
        }
    }

    public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T?>> factory, CancellationToken ct = default) where T : class
    {
        // Check memory cache
        if (_memoryCache.TryGetValue(key, out var entry))
        {
            entry.LastAccessTime = Interlocked.Increment(ref _accessCounter);
            return (T?)entry.Value;
        }

        // Check disk cache (if enabled)
        if (_diskCachePath != null)
        {
            var diskValue = await LoadFromDiskAsync<T>(key, ct);
            if (diskValue != null)
            {
                AddToMemoryCache(key, diskValue);
                return diskValue;
            }
        }

        // Execute factory
        var value = await factory();
        if (value == null) return value;
        
        // Add to the memory cache 
        AddToMemoryCache(key, value);

        // Add to the disk cache
        if (_diskCachePath != null)
        {
            await SaveToDiskAsync(key, value, ct);
        }
        
        return value;
    }

    public void Invalidate(string prefix)
    {
        var keysToRemove = _memoryCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _memoryCache.TryRemove(key, out _);
        }

        if (_diskCachePath == null) return;
        
        try
        {
            foreach (var file in Directory.GetFiles(_diskCachePath))
            {
                try
                {
                    File.Delete(file);
                }
                catch(Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to remove file from disk cache: {file}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear disk cache");
        }
    }

    public void Clear()
    {
        _memoryCache.Clear();

        if (_diskCachePath == null) return;
        
        try
        {
            foreach (var file in Directory.GetFiles(_diskCachePath))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete disk cache file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to clear disk cache");
        }
    }

    private void AddToMemoryCache(string key, object value)
    {
        var entry = new CacheEntry
        {
            Value = value,
            LastAccessTime = Interlocked.Increment(ref _accessCounter),
            CachedAt = DateTimeOffset.UtcNow
        };

        _memoryCache.TryAdd(key, entry);

        // LRU eviction if memory cache is full
        if (_memoryCache.Count > _maxMemoryCacheItems)
        {
            EvictLeastRecentlyUsed();
        }
    }

    public DateTimeOffset? GetCachedAt(string key)
    {
        if (_memoryCache.TryGetValue(key, out var entry))
        {
            return entry.CachedAt;
        }
        
        if (_diskCachePath != null)
        {
            var path = GetCacheFilePath(key);
            if (File.Exists(path))
            {
                try
                {
                    return File.GetLastWriteTimeUtc(path);
                }
                catch
                {
                    // Ignore
                }
            }
        }
        
        return null;
    }
    
    public bool Contains(string key)
    {
        if (_memoryCache.ContainsKey(key))
            return true;
        
        if (_diskCachePath != null)
        {
            var path = GetCacheFilePath(key);
            return File.Exists(path);
        }
        
        return false;
    }
    
    private static string Sha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
    
    private void EvictLeastRecentlyUsed()
    {
        try
        {
            // Find 10% of entries to evict (batch eviction for efficiency)
            var evictCount = Math.Max(1, _maxMemoryCacheItems / 10);

            var oldestEntries = _memoryCache
                .OrderBy(kvp => kvp.Value.LastAccessTime)
                .Take(evictCount)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestEntries)
            {
                _memoryCache.TryRemove(key, out _);
            }

            _logger?.LogDebug("Evicted {Count} entries from memory cache", oldestEntries.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to evict entries from memory cache");
        }
    }

    private async Task<T?> LoadFromDiskAsync<T>(string key, CancellationToken ct) where T : class
    {
        var path = GetCacheFilePath(key);
        if (!File.Exists(path)) return null;

        await _diskLock.WaitAsync(ct);
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonContext.Options, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load from disk cache: {Key}", key);
            try { File.Delete(path); }
            catch
            {
            }

            return null;
        }
        finally
        {
            _diskLock.Release();
        }
    }

    private async Task SaveToDiskAsync<T>(string key, T value, CancellationToken ct)
    {
        var path = GetCacheFilePath(key);

        await _diskLock.WaitAsync(ct);
        try
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, value, _jsonContext.Options, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save to disk cache: {Key}", key);
        }
        finally
        {
            _diskLock.Release();
        }
    }
    
    private string GetCacheFilePath(string key)
    {
        var hex = Sha256Hex(key);
        return Path.Combine(_diskCachePath!, $"{hex}.json");
    }
    
    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "default";

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_');

        return sb.ToString();
    }
    
}
