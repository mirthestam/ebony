using Aria.Core;
using Aria.Core.Connection;
using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Player;
using Aria.Core.Queue;
using Aria.Infrastructure.Caching;
using Aria.Infrastructure.Connection;
using CommunityToolkit.Mvvm.Messaging;

namespace Aria.Infrastructure;

public class AriaEngine(
    IEnumerable<IBackendConnectionFactory> integrationProviders,
    IMessenger messenger,
    IConnectionProfileProvider connectionProfileProvider) : IAriaControl, IAria
{
    // The proxy implementations are wrappers around the backend implementations, so we can easily exchange them at runtime, without having the UI to rebind to events.
    private readonly LibraryProxy _libraryProxy = new();
    private readonly PlayerProxy _playerProxy = new();
    private readonly QueueProxy _queueProxy = new();

    public event EventHandler<EngineStateChangedEventArgs>? StateChanged;
    
    public async Task RunInspectionAsync()
    {
        await _libraryProxy.InspectLibraryAsync();
    }

    public EngineState State { get; private set; } = EngineState.Stopped;

    private ScopedBackendConnection? _backendScope;

    public IPlayer Player => _playerProxy;
    public IQueue Queue => _queueProxy;
    public ILibrary Library => _libraryProxy;

    private ResourceCacheLibrarySource? _resourceCache;
    private QueryCacheLibrarySource? _infoCache;

    public Task InitializeAsync()
    {
        // Forward events from the proxies over the messenger for the UI
        // We ignore events if the engine is not in ready state yet.
        _playerProxy.StateChanged += (_, args) =>
        {
            if (State != EngineState.Ready) return;
            messenger.Send(new PlayerStateChangedMessage(args.Flags));
        };
        _libraryProxy.Updated += ( _, args) =>
        {
            if (State != EngineState.Ready) return;            
            messenger.Send(new LibraryUpdatedMessage(args.Flags));
        };
        _queueProxy.StateChanged += (_, args) =>
        {
            if (State != EngineState.Ready) return;            
            messenger.Send(new QueueStateChangedMessage(args.Flags));
        };
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        await InternalDisconnectAsync();
    }

    public Id Parse(string id)
    {
        // We need the ID from the connection to parse it here.
        // This method exists to avoid exposing the entire provider.
        return _backendScope?.Connection.IdProvider.Parse(id) ?? Id.Empty;
    }

    public async Task StartAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profiles = await connectionProfileProvider.GetAllProfilesAsync(cancellationToken).ConfigureAwait(false);
        var profile = profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) throw new InvalidOperationException("No profile found with the given ID");
        await StartAsync(profile, cancellationToken).ConfigureAwait(false);
    }

    public async Task StartAsync(IConnectionProfile connectionProfile, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the backend that is capable of handling this connection profile.
            var provider = integrationProviders.FirstOrDefault(p => p.CanHandle(connectionProfile));
            if (provider == null) throw new NotSupportedException("No provider found for connection profile");

            await InternalDisconnectAsync().ConfigureAwait(false);

            SetState(EngineState.Starting);

            // Instantiate the backend and connect our session wrappers to the actual backend implementation  
            _backendScope = await provider.CreateAsync(connectionProfile).ConfigureAwait(false);

            var backend = _backendScope.Connection;
            backend.ConnectionStateChanged += BackendOnConnectionStateChanged;

            _playerProxy.Attach(backend.Player);
            _queueProxy.Attach(backend.Queue);

            // Wrap the backend library with its caches
            _resourceCache = new ResourceCacheLibrarySource(backend.Library, connectionProfile.Id.ToString(),
                TimeSpan.FromDays(30));
            _infoCache = new QueryCacheLibrarySource(_resourceCache, TimeSpan.FromDays(5));

            _libraryProxy.Attach(_infoCache);

            //  Initialize the backend. This is where it will connect.
            await backend.ConnectAsync(cancellationToken).ConfigureAwait(false);
            
            
            SetState(EngineState.Seeding);
            
            await _libraryProxy.GetArtistsAsync(cancellationToken).ConfigureAwait(false);
            
            SetState(EngineState.Ready);
            
        }
        catch
        {
            await InternalDisconnectAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task InternalDisconnectAsync()
    {
        if (State == EngineState.Stopped) return;
        
        SetState(EngineState.Stopping);
        if (_backendScope == null)
        {
            SetState(EngineState.Stopped);
            return;
        }

        _playerProxy.Detach();
        _queueProxy.Detach();
        _libraryProxy.Detach();
        _infoCache?.Dispose();

        var connection = _backendScope.Connection;

        await connection.DisconnectAsync().ConfigureAwait(false);

        // Unbind after disconnecting; otherwise the disconnect event will never be caught.
        connection.ConnectionStateChanged -= BackendOnConnectionStateChanged;

        if (_backendScope != null)
        {
            _backendScope.Dispose();
            _backendScope = null;            
        }
        SetState(EngineState.Stopped);
    }

    private async void BackendOnConnectionStateChanged(BackendConnectionState state)
    {
        try
        {
            if (state == BackendConnectionState.Disconnected)
            {
                // For some reason, the backend is disconnected.
                // It might have lost its connection
                await InternalDisconnectAsync();
            }
        }
        catch
        {
            SetState(EngineState.Stopped);
        }
    }

    private void SetState(EngineState newState)
    {
        if (State == newState) return;
        State = newState;
        StateChanged?.Invoke(this, new EngineStateChangedEventArgs(newState));
    }
}