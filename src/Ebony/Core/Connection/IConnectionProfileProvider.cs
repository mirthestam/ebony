namespace Ebony.Core.Connection;

public interface IConnectionProfileProvider
{
    /// <summary>
    /// Gets all the available connection profiles
    /// </summary>
    Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the connection that the user configured as default.
    /// </summary>
    Task<IConnectionProfile?> GetDefaultProfileAsync();

    /// <summary>
    /// Saves this profile on disk
    /// </summary>
    Task SaveProfileAsync(IConnectionProfile profile);

    /// <summary>
    /// Makes the existing profile persistent
    /// </summary>
    Task PersistProfileAsync(Guid id);
    
    /// <summary>
    /// Removes this profile from persistent storage
    /// </summary>
    Task DeleteProfileAsync(Guid id);
    
    /// <summary>
    /// Occurs when a discovery has occurred and the profile list has been updated.
    /// </summary>
    event EventHandler DiscoveryCompleted;

    Task DiscoverAsync(CancellationToken cancellationToken = default);
    
    Task<IConnectionProfile?> GetProfileAsync(Guid connectionId);
}