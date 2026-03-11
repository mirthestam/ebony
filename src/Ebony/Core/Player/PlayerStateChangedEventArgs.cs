namespace Ebony.Core.Player;

public class PlayerStateChangedEventArgs(PlayerStateChangedFlags flags) : EventArgs
{
    public PlayerStateChangedFlags Flags { get; } = flags;
}