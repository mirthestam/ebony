namespace Ebony.Backends.MPD.Connection;

public record ConnectionConfig(string Socket, bool UseSocket, string Host, int Port, string Password);