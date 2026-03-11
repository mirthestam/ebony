using Ebony.Core.Extraction;

namespace Ebony.Features.Player.Queue;

public class EnqueueRequestedEventArgs(Id id, uint index) : EventArgs
{
    public Id Id { get; } = id;
    public uint Index { get; } = index;
}