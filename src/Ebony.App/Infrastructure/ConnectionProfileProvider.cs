using Ebony.Backends.MPD.Connection;
using Ebony.Core.Connection;
using Zeroconf;

namespace Ebony.App.Infrastructure;

public class ConnectionProfileProvider(DiskConnectionProfileSource diskSource) : IConnectionProfileProvider
{
    private readonly List<IConnectionProfile> _connectionProfiles = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isLoaded;

    public event EventHandler? DiscoveryCompleted;

    public async Task<IConnectionProfile?> GetProfileAsync(Guid connectionId)
    {
        var profiles = await GetAllProfilesAsync();
        return profiles.FirstOrDefault(p => p.Id == connectionId);
    }

    public async Task<IEnumerable<IConnectionProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_isLoaded) return _connectionProfiles.ToList();
            
            var stored = await diskSource.LoadAllAsync();
            cancellationToken.ThrowIfCancellationRequested();
            
            _connectionProfiles.AddRange(stored);
            _isLoaded = true;
            
            return _connectionProfiles.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveProfileAsync(IConnectionProfile profile)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = _connectionProfiles.FirstOrDefault(p => p.Id == profile.Id);
            if (existing != null)
            {
                _connectionProfiles.Remove(existing);
            }
            _connectionProfiles.Add(profile);
            await DiskConnectionProfileSource.SaveAsync(profile);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PersistProfileAsync(Guid id)
    {
        var profile = _connectionProfiles.FirstOrDefault(p => p.Id == id);
        
        if (profile == null) throw new InvalidOperationException("No profile found with the given ID");
        
        profile.Flags &= ~ConnectionFlags.Discovered;
        profile.Flags |= ConnectionFlags.Saved;
        
        await SaveProfileAsync(profile);
    }

    public async Task DeleteProfileAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            _connectionProfiles.RemoveAll(p => p.Id == id);
            DiskConnectionProfileSource.Delete(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IConnectionProfile?> GetDefaultProfileAsync()
    {
        var profiles = await GetAllProfilesAsync();
        return profiles
            .Where(p => p.Flags.HasFlag(ConnectionFlags.Saved))
            .OrderBy(p => p.Name)
            .FirstOrDefault(p => p.AutoConnect);
    }

    public async Task DiscoverAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await DiscoverServersAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
        
        if (!cancellationToken.IsCancellationRequested) DiscoveryCompleted?.Invoke(this, EventArgs.Empty);
    }
    
    private async Task DiscoverServersAsync(CancellationToken cancellationToken = default)
    {
        var discoveredProfiles = new List<IConnectionProfile>();
        ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync(cancellationToken: cancellationToken);            
    
        var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key), cancellationToken: cancellationToken);            
        foreach (var resp in responses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_connectionProfiles.Any(a => a.Name == resp.DisplayName))
            {
                // This connection is already saved with this name.
                // We prefer the user his override.
                continue;
            }
            
            var mpdService = resp.Services.FirstOrDefault(s => s.Key.Contains("_mpd."));
            if (mpdService.Value == null) continue;
            
            var profile = new ConnectionProfile
            {
                Id = Guid.NewGuid(),
                AutoConnect = true,
                Host = resp.IPAddress,
                Name = resp.DisplayName,
                Port = mpdService.Value.Port,
                Flags = ConnectionFlags.Discovered 
            };
            discoveredProfiles.Add(profile);
        }

        // Clear earlier discovered entries to avoid duplicates
        _connectionProfiles.RemoveAll(p => p.Flags.HasFlag(ConnectionFlags.Discovered));
        
        // Add the fresh batch
        _connectionProfiles.AddRange(discoveredProfiles);       
    }
}