using Ebony.Core.Connection;
using GObject;
using Object = GObject.Object;

namespace Ebony.Features.Shell.Welcome;

[Subclass<Object>]
public partial class ConnectionModel
{
    public static ConnectionModel FromConnectionProfile(IConnectionProfile profile)
    {
        var details = profile.AutoConnect && profile.Flags.HasFlag(ConnectionFlags.Saved) 
            ? "Auto-Connect" 
            : string.Empty;
        
        var model = NewWithProperties([]);
        model.Id = profile.Id;
        model.DisplayName = profile.Name;
        model.Details = details;
        model.IsDiscovered = profile.Flags.HasFlag(ConnectionFlags.Discovered);

        return model;
    }
    
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; }
    public string Details { get; private set; }
    public bool IsDiscovered { get; private set; }
}