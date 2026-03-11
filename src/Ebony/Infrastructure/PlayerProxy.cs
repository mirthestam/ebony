using Ebony.Core.Extraction;
using Ebony.Core.Player;

namespace Ebony.Infrastructure;

/// <summary>
/// Simple decorator for actual backend instances, providing a fallback when no backend is loaded.
/// </summary>
public class PlayerProxy : IPlayerSource
{
    //  Simple decorator because underlying integrations can change
    private IPlayerSource? _innerPlayer;

    public event EventHandler<PlayerStateChangedEventArgs>? StateChanged;    
    
    public Task PlayAsync(uint index)
    {
        return _innerPlayer?.PlayAsync(index) ?? Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        return _innerPlayer?.PauseAsync() ?? Task.CompletedTask;
    }
    
    public Task ResumeAsync()
    {
        return _innerPlayer?.ResumeAsync() ?? Task.CompletedTask;
    }    

    public Task NextAsync()
    {
        return _innerPlayer?.NextAsync() ?? Task.CompletedTask;
    }

    public Task PreviousAsync()
    {
        return _innerPlayer?.PreviousAsync() ?? Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return _innerPlayer?.StopAsync() ?? Task.CompletedTask;
    }

    public Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        return _innerPlayer?.SeekAsync(position, cancellationToken) ?? Task.CompletedTask;
    }

    public Task SetVolumeAsync(int volume)
    {
        return _innerPlayer?.SetVolumeAsync(volume) ?? Task.CompletedTask;
    }

    public Id Id => _innerPlayer?.Id ?? null!;

    public bool SupportsVolume => _innerPlayer?.SupportsVolume ?? false;
    public PlaybackState State => _innerPlayer?.State ?? PlaybackState.Unknown;
    public int? XFade => _innerPlayer?.XFade;
    public bool CanXFade => _innerPlayer?.CanXFade ?? false;
    public int? Volume => _innerPlayer?.Volume;
    public PlaybackProgress Progress => _innerPlayer?.Progress ?? new PlaybackProgress();

    internal void Attach(IPlayerSource player)
    {
        if (_innerPlayer != null) Detach();
        _innerPlayer = player;
        _innerPlayer.StateChanged += InnerPlayerOnStateChanged;
    }
    
    internal void Detach()
    {
        if (_innerPlayer != null)
        {
            _innerPlayer.StateChanged -= InnerPlayerOnStateChanged;
        }
        _innerPlayer = null;
    }
    
    private void InnerPlayerOnStateChanged(object? sender, PlayerStateChangedEventArgs args)
    {
        StateChanged?.Invoke(sender, args);
    }
}