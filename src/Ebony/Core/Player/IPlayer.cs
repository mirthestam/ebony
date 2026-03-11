using Ebony.Core.Extraction;

namespace Ebony.Core.Player;

/// <summary>
/// Controls the player and provides basic information about its state.
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// The identity of the player. Some backends support having multiple players. 
    /// </summary>
    public Id Id { get; }

    public int? Volume { get; }

    public bool SupportsVolume { get; }

    public PlaybackState State { get; }

    /// <summary>
    ///     The number of seconds to Crossfaded between Track changes
    /// </summary>
    public int? XFade { get; }

    /// <summary>
    ///     Whether this player supports crossfading
    /// </summary>
    public bool CanXFade { get; }

    public PlaybackProgress Progress { get; }
    
    /// <summary>
    /// Starts playback of the current index in the queue.
    /// </summary>
    Task PlayAsync(uint index);

    /// <summary>
    /// Pauses the playback if the player is currently playing.
    /// </summary>
    Task PauseAsync();
    
    /// <summary>
    /// Resumes playback if the player is currently paused.
    /// </summary>
    /// <returns></returns>
    Task ResumeAsync();    
    
    /// <summary>
    /// Skips to the next track in the playlist.
    /// </summary>
    /// <returns></returns>
    Task NextAsync();

    /// <summary>
    /// Skips to the previous track in the playlist.
    /// </summary>
    /// <returns></returns>
    Task PreviousAsync();

    /// <summary>
    /// Stops playback and resets the player to the beginning of the current track. 
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Seeks to the specified position in the current track.
    /// </summary>
    Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default);

    Task SetVolumeAsync(int volume);
}