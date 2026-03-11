namespace Ebony.Backends.MPD.Connection;

public class IdleResponseEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}