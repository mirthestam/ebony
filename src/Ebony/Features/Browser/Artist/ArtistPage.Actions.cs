using Ebony.Core.Queue;
using Ebony.Infrastructure;
using Gio;
using GLib;
using Gtk;

namespace Ebony.Features.Browser.Artist;

public partial class ArtistPage
{
    // Actions
    private SimpleAction _albumShowAction;
    private SimpleAction _albumShowForArtistAction;
    private SimpleAction _enqueueReplaceAction;
    private SimpleAction _enqueueNextAction;
    private SimpleAction _enqueueEndAction;

    private void InitializeActions()
    {
        const string albumsGroup = "albums";
        var albumsActionGroup = SimpleActionGroup.New();
        _sorterAction = SimpleAction.NewStateful("sorting", VariantType.String, Variant.NewString("Title"));
        albumsActionGroup.AddAction(_sorterAction);
        InsertActionGroup(albumsGroup, albumsActionGroup);        
        
        const string group = "album";
        const string showAlbum = "show-album";
        const string showAlbumForArtist = "show-album-for-artist";
        const string enqueueReplace = "enqueue-replace";
        const string enqueueNext = "enqueue-next";
        const string enqueueEnd = "enqueue-end";

        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_albumShowAction = SimpleAction.New(showAlbum, null));
        queueActionGroup.AddAction(_albumShowForArtistAction = SimpleAction.New(showAlbumForArtist, null));
        queueActionGroup.AddAction(_enqueueReplaceAction = SimpleAction.New(enqueueReplace, null));
        queueActionGroup.AddAction(_enqueueNextAction = SimpleAction.New(enqueueNext, null));
        queueActionGroup.AddAction(_enqueueEndAction = SimpleAction.New(enqueueEnd, null));
        _albumShowAction.OnActivate += AlbumShowActionOnOnActivate;
        _albumShowForArtistAction.OnActivate += AlbumShowForArtistActionOnOnActivate;
        _enqueueReplaceAction.OnActivate += EnqueueReplaceActionOnOnActivate;
        _enqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _enqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        InsertActionGroup(group, queueActionGroup);

        var defaultAction = IQueue.DefaultEnqueueAction switch
        {
            EnqueueAction.Replace => enqueueReplace,
            EnqueueAction.EnqueueNext => enqueueNext,
            EnqueueAction.EnqueueEnd => enqueueEnd,
            _ => throw new ArgumentOutOfRangeException()
        };

        // This is going to be a problem the moment the user is able to change his default,
        // as in that case we to set another item as default.
        var controller = ShortcutController.NewWithProperties([]);
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Return"),
            NamedAction.New($"{group}.{showAlbumForArtist}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"),
            NamedAction.New($"{group}.{defaultAction}")));
        AddController(controller);

        var menu = Menu.NewWithProperties([]);

        menu.AppendItem(MenuItem.New("Show Album for artist", $"{group}.{showAlbumForArtist}"));
        menu.AppendItem(MenuItem.New("Show Album", $"{group}.{showAlbum}"));

        var enqueueMenu = Menu.NewWithProperties([]);

        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", $"{group}.{enqueueReplace}");
        enqueueMenu.AppendItem(replaceQueueItem);

        var playNextItem = MenuItem.New("Play after current track", $"{group}.{enqueueNext}");
        enqueueMenu.AppendItem(playNextItem);

        var playLastItem = MenuItem.New("Add to queue", $"{group}.{enqueueEnd}");
        enqueueMenu.AppendItem(playLastItem);

        menu.AppendSection(null, enqueueMenu);

        _albumsGrid.SetAlbumMenu(menu);
    }

    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _albumsGrid.GetSelected();
        if (item == null) return;

        var argumentArray = item.AlbumId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argumentArray);
    }

    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _albumsGrid.GetSelected();
        if (item == null) return;
        
        var argumentArray = item.AlbumId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argumentArray);
    }

    private void EnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _albumsGrid.GetSelected();
        if (item == null) return;
        
        var argumentArray = item.AlbumId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argumentArray);
    }

    private void AlbumShowActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _albumsGrid.GetSelected();
        if (item == null) return;
        
        var argument = item.AlbumId.ToVariant();
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", argument);        
    }    

    private void AlbumShowForArtistActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _albumsGrid.GetSelected();
        if (item == null) return;

        var argument = item.AlbumId.ToVariant();        
        
        var parameters = Variant.NewArray(VariantType.String, [
            argument,
            _artist.Id.ToVariant()
        ]);

        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbumForArtist.Action}", parameters);
    }
}