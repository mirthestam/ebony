using Ebony.Infrastructure;

namespace Ebony.Core.Queue;

public readonly record struct RepeatSettings
{
    public required RepeatMode Mode { get; init; } 
    public required bool Supported { get; init; }
    
    public static RepeatSettings Default => new()
    {
        Mode = RepeatMode.Disabled,
        Supported = false
    };
}