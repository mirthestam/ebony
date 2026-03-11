using GObject;
using Gtk;

namespace Ebony.Features.Browser.Artists;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(ArtistListItem)}.ui")]
public partial class ArtistListItem
{
    [Connect("name-label")] private Label _nameLabel;

    public void Update(ArtistModel model)
    {
        switch (model.NameDisplay)
        {
            case ArtistNameDisplay.Name:
                TooltipText = model.Artist.Name;
                _nameLabel.SetLabel(model.Artist.Name);                
                break;
            case ArtistNameDisplay.NameSort:
                var name = model.Artist.NameSort ?? model.Artist.Name;
                TooltipText = name;
                _nameLabel.SetLabel(name);                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        

    }
}