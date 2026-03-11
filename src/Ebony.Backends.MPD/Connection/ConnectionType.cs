namespace Ebony.Backends.MPD.Connection;

public enum ConnectionType
{
    /// <summary>
    /// Dedicated connection for periodic status polling
    /// </summary>
    Status,

    /// <summary>
    /// Dedicated connection using MPD's IDLE command for event-driven updates
    /// </summary>
    Idle,
    
    /// <summary>
    ///  Pool of connections available for executing MPD commands from the application
    /// </summary>
    Pool
}