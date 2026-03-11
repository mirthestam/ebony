using Ebony.Core.Extraction;

namespace Ebony.Core.Player;

public class PlaybackProgress
{
    public Id? Id { get; set; }

    public TimeSpan Elapsed { get; set; }

    public TimeSpan Duration { get; set; }

    public TimeSpan Remaining => Duration - Elapsed;

    /// <summary>
    ///     i.e.,  16, 24 (-bit)
    /// </summary>
    public int AudioBits { get; set; }

    /// <summary>
    ///     i.e. 2, 6 (channels)
    /// </summary>
    public int AudioChannels { get; set; }

    /// <summary>
    ///     i.e. 44100, 96000 (hz)
    /// </summary>
    public int AudioSampleRate { get; set; }

    /// <summary>
    ///     i.e. 320 (kbps)
    /// </summary>
    public int Bitrate { get; set; }
    
    public static PlaybackProgress Default { get; } = new();
}