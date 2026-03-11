namespace Ebony.Core.Queue;

public readonly record struct ConsumeSettings
{
    public required bool Enabled { get; init; }
    public required bool Supported { get; init; }
    
    public static ConsumeSettings Default => new()
    {
        Enabled = false,
        Supported = false
    };
}