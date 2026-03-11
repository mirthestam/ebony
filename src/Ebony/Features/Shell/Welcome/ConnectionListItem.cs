using Adw;
using GObject;
using Gtk;

namespace Ebony.Features.Shell.Welcome;

[Subclass<ActionRow>]
[Template<AssemblyResource>($"ui/{nameof(ConnectionListItem)}.ui")]
public partial class ConnectionListItem
{
    [Connect("title-label")] private Label _titleLabel;
    [Connect("subtitle-label")] private Label _subtitleLabel;
    [Connect("discovered-label")] private Label _discoveredLabel;

    public Guid ConnectionId { get; private set; }

    public static ConnectionListItem NewFromModel(ConnectionModel model)
    {
        var item = NewWithProperties([]);
        item.Parse(model);
        return item;
    }

    private void Parse(ConnectionModel model)
    {
        ConnectionId = model.Id;
        _titleLabel.SetLabel(model.DisplayName);
        _subtitleLabel.SetLabel(model.Details);
        _discoveredLabel.SetVisible(model.IsDiscovered);    
    }
}