using System;

namespace Aria.Infrastructure.Caching;

public interface ILibraryCache
{
    Task<T?> GetOrAddAsync<T>(string key, Func<Task<T?>> factory, CancellationToken ct = default) where T : class;
    void Invalidate(string prefix);
}