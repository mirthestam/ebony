namespace Ebony.Core.Queue;

public readonly record struct PlaybackOrder
{
    public uint? CurrentIndex { get; init; }
    public required bool HasNext { get; init; }
    
    public static PlaybackOrder Default => new()
    {
        CurrentIndex = null,
        HasNext = false
    };
}