using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Queue;

namespace Ebony.Infrastructure;

public abstract class BaseQueue : IQueueSource
{
    public virtual event EventHandler<QueueStateChangedEventArgs>? StateChanged;
    
    public virtual Id Id { get; protected set; } = Id.Empty;

    public virtual uint Length  { get; protected set; }

    public virtual PlaybackOrder Order { get; protected set; } = PlaybackOrder.Default;

    public virtual ShuffleSettings Shuffle  { get; protected set; }

    public virtual RepeatSettings Repeat  { get; protected set; }

    public virtual ConsumeSettings Consume  { get; protected set; }

    public virtual QueueMode Mode => QueueMode.Playlist;

    public virtual QueueTrackInfo? CurrentTrack  { get; protected set; }

    public virtual Task SetShuffleAsync(bool enabled) => Task.CompletedTask;

    public virtual Task SetRepeatAsync(RepeatMode repeatMode) => Task.CompletedTask;

    public virtual Task SetConsumeAsync(bool enabled) => Task.CompletedTask;

    public virtual IEnumerable<QueueTrackInfo> Tracks => [];
    
    public virtual Task EnqueueAsync(Info item, EnqueueAction action) => Task.CompletedTask;
    
    public virtual Task EnqueueAsync(IEnumerable<Info> items, EnqueueAction action) => Task.CompletedTask;

    public virtual Task EnqueueAsync(Info item, uint index) => Task.CompletedTask;

    public virtual Task MoveAsync(Id sourceTrackId, uint targetPlaylistIndex) => Task.CompletedTask;

    public virtual Task ClearAsync() => Task.CompletedTask;

    public abstract Task SaveOrAppendToPlaylistAsync(string playlistName);

    public virtual Task RemoveTrackAsync(Id id) => Task.CompletedTask;

    protected void OnStateChanged(QueueStateChangedFlags flags)
    {
        StateChanged?.Invoke(this, new QueueStateChangedEventArgs(flags));
    }        
}