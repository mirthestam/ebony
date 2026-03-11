using MpcNET;

namespace Ebony.Backends.MPD.Connection;

public class StatusChangedEventArgs(MpdStatus status) : EventArgs
{
    public MpdStatus Status { get; } = status;
}