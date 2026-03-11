namespace Ebony.Core.Queue;

public class QueueStateChangedEventArgs(QueueStateChangedFlags flags) : EventArgs
{
    public QueueStateChangedFlags Flags { get; } = flags;
}