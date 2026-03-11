using Ebony.Core.Library;
using Ebony.Core.Player;
using Ebony.Core.Queue;

namespace Ebony.Infrastructure;

public interface IPlayerSource : IPlayer
{
    event EventHandler<PlayerStateChangedEventArgs>? StateChanged;    
}

public interface IQueueSource : IQueue
{
    event EventHandler<QueueStateChangedEventArgs>? StateChanged;    
}

public interface ILibrarySource : ILibrary
{
    public event EventHandler<LibraryChangedEventArgs>? Updated;
    Task InspectLibraryAsync(CancellationToken ct = default);
}