using Ebony.Core.Extraction;

namespace Ebony.Features.Player.Queue;

public class MoveRequestedEventArgs(Id sourceId, uint targetIndex) : EventArgs
{
    public Id SourceId { get; } = sourceId;
    public uint TargetIndex { get; } = targetIndex;
}