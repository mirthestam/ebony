using Ebony.Core.Extraction;
using Ebony.Core.Player;

namespace Ebony.Infrastructure;

public abstract class BasePlayer : IPlayerSource
{
    public virtual event EventHandler<PlayerStateChangedEventArgs>? StateChanged;
    
    public virtual Id Id { get; protected set;  } = Id.Empty;

    public virtual int? Volume { get; protected set; } = null;
    
    public virtual bool SupportsVolume => false;
    
    public virtual PlaybackState State  { get; protected set;  } = PlaybackState.Stopped;
    
    public virtual int? XFade => null;
    
    public virtual bool CanXFade => false;
    
    public virtual PlaybackProgress Progress => PlaybackProgress.Default;

    public virtual Task PlayAsync(uint index) => Task.CompletedTask;

    public virtual Task PauseAsync() => Task.CompletedTask;

    public virtual Task NextAsync() => Task.CompletedTask;
    
    public virtual Task ResumeAsync() => Task.CompletedTask;

    public virtual Task PreviousAsync() => Task.CompletedTask;

    public virtual Task StopAsync() => Task.CompletedTask;
    
    public virtual Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    public virtual Task SetVolumeAsync(int volume) => Task.CompletedTask;

    protected void OnStateChanged(PlayerStateChangedFlags flags)
    {
        StateChanged?.Invoke(this, new PlayerStateChangedEventArgs(flags));
    }    
}