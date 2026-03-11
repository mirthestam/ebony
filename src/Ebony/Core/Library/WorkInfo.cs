namespace Ebony.Core.Library;

public record WorkInfo
{
    /// <summary>
    ///     a work is a distinct intellectual or artistic creation, which can be expressed in the form of one or more audio
    ///     recordings
    /// </summary>
    public required string Work { get; init; }

    /// <summary>
    ///     name of the movement, e.g. “Andante con moto”.
    /// </summary>
    public required string MovementName { get; init; }

    /// <summary>
    ///     movement number, e.g. “2” or “II”.
    /// </summary>
    public required string MovementNumber { get; init; }

    /// <summary>
    ///     Instructs the player to display  the work, movement, and movement number instead of trac title
    /// required </summary>
    public bool ShowMovement { get; init; }
}