namespace Ebony.Core.Connection;

[Flags]
public enum ConnectionFlags
{
    None = 0,
    /// <summary>
    /// Indicates that this connection as been discovered using service discovery
    /// </summary>
    Discovered = 1 << 0,
    
    /// <summary>
    /// Indicates that the user has saved this connection
    /// </summary>
    Saved = 1 << 1
}