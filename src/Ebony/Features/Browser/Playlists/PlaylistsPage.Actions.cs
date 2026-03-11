using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure;
using Gdk;
using Gio;
using Gtk;
using AlertDialog = Adw.AlertDialog;

namespace Ebony.Features.Browser.Playlists;

public class RenamePlaylistEventArgs : EventArgs
{
    public Id PlaylistId { get; }
    public string PlaylistName { get; set; } = "";
    
    public RenamePlaylistEventArgs(Id playlistId, string newName)
    {
        PlaylistId = playlistId;
        PlaylistName = newName;       
    }
}


public partial class PlaylistsPage
{
    [Connect("confirm-playlist-delete")] private AlertDialog _confirmPlaylistDeleteDialog;
    
    // Actions
    private SimpleAction _showAction;
    private SimpleAction _enqueueDefaultAction;    
    private SimpleAction _enqueueReplaceAction;
    private SimpleAction _enqueueNextAction;
    private SimpleAction _enqueueEndAction;

    private SimpleAction _renameAction;
    private SimpleAction _deleteAction;

    private const string Group = "playlist";
    private const string ActionShowItem = "show";
    private const string ActionRenameItem = "rename";
    private const string ActionDeleteItem = "delete";
    private const string ActionEnqueueDefault = "enqueue-default";        
    private const string ActionEnqueueReplace = "enqueue-replace";
    private const string ActionEnqueueNext = "enqueue-next";
    private const string ActionEnqueueEnd = "enqueue-end";
    
    public event EventHandler<Id>? DeleteRequested;    
    public event EventHandler<RenamePlaylistEventArgs>? RenameRequested;   
    
    private void InitializeActions()
    {
        var itemActionGroup = SimpleActionGroup.New();
        itemActionGroup.AddAction(_showAction = SimpleAction.New(ActionShowItem, null));
        itemActionGroup.AddAction(_enqueueDefaultAction = SimpleAction.New(ActionEnqueueDefault, null));        
        itemActionGroup.AddAction(_enqueueReplaceAction = SimpleAction.New(ActionEnqueueReplace, null));
        itemActionGroup.AddAction(_enqueueNextAction = SimpleAction.New(ActionEnqueueNext, null));
        itemActionGroup.AddAction(_enqueueEndAction = SimpleAction.New(ActionEnqueueEnd, null));
        itemActionGroup.AddAction(_renameAction = SimpleAction.New(ActionRenameItem, null));       
        itemActionGroup.AddAction(_deleteAction = SimpleAction.New(ActionDeleteItem, null));
        
        _showAction.OnActivate += ShowActionOnOnActivate;
        _enqueueDefaultAction.OnActivate += EnqueueDefaultActionOnOnActivate;
        _enqueueReplaceAction.OnActivate += EnqueueReplaceActionOnOnActivate;
        _enqueueNextAction.OnActivate += EnqueueNextActionOnOnActivate;
        _enqueueEndAction.OnActivate += EnqueueEndActionOnOnActivate;
        _deleteAction.OnActivate += DeleteActionOnOnActivate;
        _renameAction.OnActivate += RenameActionOnOnActivate;
        
        _confirmPlaylistDeleteDialog.OnResponse += ConfirmPlaylistDeleteDialogOnOnResponse;        
        
        InsertActionGroup(Group, itemActionGroup);
        
        ConfigureShortcuts();
        CreatePlaylistContextMenu();
    }
    
    private async void RenameActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        try
        {
            var playlist = GetSelectedPlaylist();
            if (playlist == null) return;
            
            var dialog = RenamePlaylistDialog.NewWithProperties([]);
            var result = await dialog.ShowForPlaylistAsync(playlist, NameValidator, this);
            if (!result) return;
            
            var name = dialog.PlaylistName;
            RenameRequested?.Invoke(this, new RenamePlaylistEventArgs(playlist.PlaylistId, name));
        }
        catch 
        {
            //OK
        }

    }

    private void DeleteActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        _confirmPlaylistDeleteDialog.Present(this);
    }

    private void ConfirmPlaylistDeleteDialogOnOnResponse(AlertDialog sender, AlertDialog.ResponseSignalArgs args)
    {
        var response = args.Response;
        switch (response)
        {
            case "cancel":
                return;
            
            case "delete":
                break;
        }

        DeleteRequested?.Invoke(this, GetSelectedPlaylist()!.PlaylistId);
    }
    
    private void EnqueueEndActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = GetSelectedPlaylist()!.PlaylistId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueEnd.Action}", argumentArray);        
    }

    private void EnqueueNextActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = GetSelectedPlaylist()!.PlaylistId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueNext.Action}", argumentArray);
    }

    private void EnqueueReplaceActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = GetSelectedPlaylist()!.PlaylistId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueReplace.Action}", argumentArray);
    }

    private void EnqueueDefaultActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argumentArray = GetSelectedPlaylist()!.PlaylistId.ToVariantArray();
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.EnqueueDefault.Action}", argumentArray);
    }

    private void ShowActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var argument = GetSelectedPlaylist()!.PlaylistId.ToVariant();
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowPlaylist.Action}", argument);
    }

    private void CreatePlaylistContextMenu()
    {
        var menu = Menu.NewWithProperties([]);
        //menu.AppendItem(MenuItem.New("Show Playlist", $"{Group}.{ActionShowItem}"));
        
        var enqueueMenu = Menu.NewWithProperties([]);
        var replaceQueueItem = MenuItem.New("Play now (Replace queue)", $"{Group}.{ActionEnqueueReplace}");
        enqueueMenu.AppendItem(replaceQueueItem);
        
        var playNextItem = MenuItem.New("Play after current track", $"{Group}.{ActionEnqueueNext}");
        enqueueMenu.AppendItem(playNextItem);        
        
        var playLastItem = MenuItem.New("Add to queue", $"{Group}.{ActionEnqueueEnd}");
        enqueueMenu.AppendItem(playLastItem);
        
        menu.AppendSection(null, enqueueMenu);        

        var suffixMenu = Menu.NewWithProperties([]);
        
        var renameItem = MenuItem.New("Rename", $"{Group}.{ActionRenameItem}");
        var deleteItem = MenuItem.New("Delete", $"{Group}.{ActionDeleteItem}");
        suffixMenu.AppendItem(renameItem);
        suffixMenu.AppendItem(deleteItem);        
        
        menu.AppendSection(null, suffixMenu);
        
        _playlistPopoverMenu.SetMenuModel(menu);
    }

    private void ConfigureShortcuts()
    {
        var controller = ShortcutController.NewWithProperties([]);
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("F2"), NamedAction.New($"{Group}.{ActionRenameItem}")));        
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Return"), NamedAction.New($"{Group}.{ActionShowItem}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{Group}.{ActionEnqueueDefault}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Delete"), NamedAction.New($"{Group}.{ActionDeleteItem}")));
        AddController(controller);
    }
}