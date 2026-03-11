using Ebony.Core.Connection;
using Ebony.Core.Extraction;

namespace Ebony.Infrastructure.Connection;

public abstract class BaseBackendConnection(
    IPlayerSource player,
    IQueueSource queue,
    ILibrarySource library,
    IIdProvider idProvider) : IBackendConnection
{
    public event Action<BackendConnectionState>? ConnectionStateChanged;
    
    public IPlayerSource Player => player;
    public IQueueSource Queue => queue;
    public ILibrarySource Library => library;
    public IIdProvider IdProvider => idProvider;

    public virtual Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        OnConnectionStateChanged(BackendConnectionState.Connecting);
        OnConnectionStateChanged(BackendConnectionState.Connected);
        
        return Task.CompletedTask;
    }

    public virtual Task DisconnectAsync()
    {
        OnConnectionStateChanged(BackendConnectionState.Disconnected);

        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    
    protected void OnConnectionStateChanged(BackendConnectionState flags)
    {
        ConnectionStateChanged?.Invoke(flags);
    }        
}