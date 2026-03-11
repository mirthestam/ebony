namespace Ebony.Features.Player.Queue;

public class TrackActivatedEventArgs(uint selectedIndex) : EventArgs
{
    public uint SelectedIndex { get; } = selectedIndex;
}