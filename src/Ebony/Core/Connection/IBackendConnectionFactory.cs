using Ebony.Infrastructure.Connection;

namespace Ebony.Core.Connection;

public interface IBackendConnectionFactory
{
    /// <summary>
    /// Determines whether this factory is able to instantiate a backend instance that is compatible with this profile
    /// </summary>
    bool CanHandle(IConnectionProfile profile);

    /// <summary>
    /// Creates a new backend instance configured with the profile
    /// </summary>
    Task<ScopedBackendConnection> CreateAsync(IConnectionProfile profile);
}