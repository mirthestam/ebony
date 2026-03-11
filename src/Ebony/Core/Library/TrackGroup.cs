namespace Ebony.Core.Library;

public record TrackGroup
{
    /// <summary>
    /// The title of the track group to show to the user
    /// </summary>
    public required string Header { get; init; }
    
    /// <summary>
    /// They key of the track group.
    /// </summary>
    public required string Key { get; init; }
}