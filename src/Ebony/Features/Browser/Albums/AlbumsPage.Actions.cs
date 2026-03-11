using Ebony.Core.Queue;
using Ebony.Infrastructure;
using Gdk;
using Gio;
using GLib;
using Gtk;

namespace Ebony.Features.Browser.Albums;

public partial class AlbumsPage
{
    // Actions
    private SimpleAction _albumShowAction;
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
        const string enqueueReplace = "enqueue-replace";
        const string enqueueNext = "enqueue-next";
        const string enqueueEnd = "enqueue-end";
        
        var albumActionGroup = SimpleActionGroup.New();
        albumActionGroup.AddAction(_albumShowAction = SimpleAction.New(showAlbum, null));
        albumActionGroup.AddAction(_enqueueReplaceAction = SimpleAction.New(enqueueReplace, null));
        albumActionGroup.AddAction(_enqueueNextAction = SimpleAction.New(enqueueNext, null));
        albumActionGroup.AddAction(_enqueueEndAction = SimpleAction.New(enqueueEnd, null));
        
        _albumShowAction.OnActivate += AlbumShowActionOnOnActivate;
        _enqueueReplaceAction.OnActivate += EnqueueReplaceActionOnOnActivate;
        _enqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _enqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        InsertActionGroup(group, albumActionGroup);

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
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Return"), NamedAction.New($"{group}.{showAlbum}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{group}.{defaultAction}")));
        AddController(controller);
        
        var menu = Menu.NewWithProperties([]);
        menu.AppendItem(MenuItem.New("Show Album", $"{group}.{showAlbum}"));
        
        var enqueueMenu = Menu.NewWithProperties([]);
        
        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", $"{group}.{enqueueReplace}");
        enqueueMenu.AppendItem(replaceQueueItem);
        
        var playNextItem = MenuItem.New("Play after current track", $"{group}.{enqueueNext}");
        enqueueMenu.AppendItem(playNextItem);        
        
        var playLastItem = MenuItem.New("Add to queue", $"{group}.{enqueueEnd}");
        enqueueMenu.AppendItem(playLastItem);        
        
        menu.AppendSection(null, enqueueMenu);
        
        _gridView.SetAlbumMenu(menu);
    }    
    
    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _gridView.GetSelected();
        if (item == null) return;

        var argumentArray = item.AlbumId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argumentArray);
    }

    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _gridView.GetSelected();
        if (item == null) return;

        var argumentArray = item.AlbumId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argumentArray);
    }

    private void EnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _gridView.GetSelected();
        if (item == null) return;

        var argumentArray = item.AlbumId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argumentArray);
    }

    private void AlbumShowActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var item = _gridView.GetSelected();
        if (item == null) return;

        var argument = item.AlbumId.ToVariant();
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", argument);        
    }
}