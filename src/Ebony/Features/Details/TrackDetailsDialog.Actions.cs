using Ebony.Core;
using Ebony.Infrastructure;
using Gio;
using GLib;

namespace Ebony.Features.Details;

public partial class TrackDetailsDialog
{
    private SimpleAction _showAlbumAction;
    private SimpleAction _showArtistAction;

    private void InitializeActions()
    {
        var actionGroup = SimpleActionGroup.New();
        actionGroup.AddAction(_showAlbumAction = SimpleAction.New("show-album", VariantType.String));        
        actionGroup.AddAction(_showArtistAction = SimpleAction.New("show-artist", VariantType.String));

        InsertActionGroup("details", actionGroup);

        _showAlbumAction.OnActivate += ShowAlbumActionOnOnActivate;
        _showArtistAction.OnActivate += ShowArtistActionOnOnActivate;
    }
   
    private void ShowArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowArtist.Action}", args.Parameter);
        Dismiss();
    }

    private void ShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", args.Parameter);
        Dismiss();   
    }    
}