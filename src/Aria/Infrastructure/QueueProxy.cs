using Aria.Core.Extraction;
using Aria.Core.Library;
using Aria.Core.Queue;

namespace Aria.Infrastructure;

public enum RepeatMode
{
    Disabled,
    Single,
    All
}

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class QueueProxy : IQueueSource
{
    public event EventHandler<QueueStateChangedEventArgs>? StateChanged;
    
    private IQueueSource? _innerQueue;

    public Id Id => _innerQueue?.Id ?? null!;
    public uint Length => _innerQueue?.Length ?? 0;

    public PlaybackOrder Order => _innerQueue?.Order ?? PlaybackOrder.Default;
    public ShuffleSettings Shuffle => _innerQueue?.Shuffle ?? ShuffleSettings.Default;
    public RepeatSettings Repeat => _innerQueue?.Repeat ?? RepeatSettings.Default;
    public ConsumeSettings Consume => _innerQueue?.Consume ?? ConsumeSettings.Default;
    public QueueMode Mode => _innerQueue?.Mode ?? QueueMode.Playlist;

    public Task SetShuffleAsync(bool enabled) => _innerQueue?.SetShuffleAsync(enabled) ?? Task.CompletedTask;
    public Task SetRepeatAsync(RepeatMode repeatMode) => _innerQueue?.SetRepeatAsync(repeatMode) ?? Task.CompletedTask;
    public Task SetConsumeAsync(bool enabled) => _innerQueue?.SetConsumeAsync(enabled) ?? Task.CompletedTask;
    public IEnumerable<QueueTrackInfo> Tracks => _innerQueue?.Tracks ?? [];

    public QueueTrackInfo? CurrentTrack => _innerQueue?.CurrentTrack;

    public Task EnqueueAsync(Info item, EnqueueAction action) => _innerQueue?.EnqueueAsync(item, action) ?? Task.CompletedTask;
    public Task EnqueueAsync(IEnumerable<Info> items, EnqueueAction action) => _innerQueue?.EnqueueAsync(items, action) ?? Task.CompletedTask;

    public Task EnqueueAsync(Info item, uint index) => _innerQueue?.EnqueueAsync(item, index) ?? Task.CompletedTask;
    public Task MoveAsync(Id sourceTrackId, uint targetPlaylistIndex) => _innerQueue?.MoveAsync(sourceTrackId, targetPlaylistIndex) ?? Task.CompletedTask;
    public Task ClearAsync() => _innerQueue?.ClearAsync() ?? Task.CompletedTask;
    public Task SaveOrAppendToPlaylistAsync(string playlistName)
    {
        return _innerQueue?.SaveOrAppendToPlaylistAsync(playlistName) ?? Task.CompletedTask;
    }

    public Task RemoveTrackAsync(Id id) => _innerQueue?.RemoveTrackAsync(id) ?? Task.CompletedTask;

    internal void Attach(IQueueSource queue)
    {
        if (_innerQueue != null) Detach();
        _innerQueue = queue;
        _innerQueue.StateChanged += InnerQueueOnStateChanged;
    }
    
    internal void Detach()
    {
        _innerQueue?.StateChanged -= InnerQueueOnStateChanged;
        _innerQueue = null;
    }
    
    private void InnerQueueOnStateChanged(object? sender, QueueStateChangedEventArgs args)
    {
        StateChanged?.Invoke(sender, args);
    }    
}