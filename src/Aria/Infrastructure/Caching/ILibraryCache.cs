using System;

namespace Aria.Infrastructure.Caching;

public interface ILibraryCache
{
    Task<T?> GetOrAddAsync<T>(string key, Func<Task<T?>> factory, CancellationToken ct = default);
    void Invalidate(string prefix);
}