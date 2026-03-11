namespace Ebony.Core.Connection;

/// <summary>
///     Represents a configuration profile for connecting to an integration. Stores the settings and credentials.
/// </summary>
public interface IConnectionProfile
{
    public Guid Id { get; }
    /// <summary>
    /// A user-configurable display name to identify this profile.
    /// </summary>
    /// <example>My home server</example>
    /// <example>MPD (Beach House)</example>
    /// <example>Bedroom</example>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets whether this profile should be automatically connected to when the application starts.
    /// </summary>
    public bool AutoConnect { get; set; }
    
    public ConnectionFlags Flags { get; set; }
}