using Ebony.Core.Connection;

namespace Ebony.Infrastructure;

public class EngineStateChangedEventArgs(EngineState state) : EventArgs
{
    public EngineState State { get; } = state;
}