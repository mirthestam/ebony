namespace Ebony.Core.Queue;

public readonly record struct ShuffleSettings
{
    public required bool Enabled { get; init; }
    public required bool Supported { get; init; }
    
    public static ShuffleSettings Default => new()
    {
        Enabled = false,
        Supported = false
    };
}