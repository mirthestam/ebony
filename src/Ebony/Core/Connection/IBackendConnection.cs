using Ebony.Core.Extraction;
using Ebony.Infrastructure;

namespace Ebony.Core.Connection;

public interface IBackendConnection : IDisposable
{
    IPlayerSource Player { get; }

    IQueueSource Queue { get; }

    ILibrarySource Library { get; }
    
    IIdProvider IdProvider { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync();
    
    public event Action<BackendConnectionState>? ConnectionStateChanged;
}