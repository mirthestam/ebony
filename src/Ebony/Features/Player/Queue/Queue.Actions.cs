using Ebony.Infrastructure;
using Gdk;
using Gio;
using GLib;
using Gtk;

namespace Ebony.Features.Player.Queue;

public partial class Queue
{
    // Actions
    private SimpleAction? _queueDeleteSelectionAction;
    private SimpleAction? _queueShowAlbumAction;
    private SimpleAction? _queueShowTrackAction;
    
    private new void InsertActionGroup(string name, ActionGroup? actionGroup)
    {
        base.InsertActionGroup(name, actionGroup);
        _tracksListView.InsertActionGroup(name, actionGroup);
    }    
    
    private void InitializeQueueActionGroup()
    {
        const string group = "queue";
        const string deleteSelection = "delete-selection";
        const string showAlbum = "show-album";
        const string showTrack = "show-track";
        
        var queueActionGroup = SimpleActionGroup.New();
        queueActionGroup.AddAction(_queueDeleteSelectionAction = SimpleAction.New(deleteSelection, null));
        queueActionGroup.AddAction(_queueShowAlbumAction = SimpleAction.New(showAlbum, null));
        queueActionGroup.AddAction(_queueShowTrackAction = SimpleAction.New(showTrack, null));
        _queueDeleteSelectionAction.OnActivate += QueueDeleteSelectionActionOnOnActivate;
        _queueShowAlbumAction.OnActivate += QueueShowAlbumActionOnOnActivate;
        _queueShowTrackAction.OnActivate += QueueShowTrackActionOnOnActivate;
        InsertActionGroup(group, queueActionGroup);
        
        var controller = ShortcutController.New();
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("Delete"), NamedAction.New($"{group}.{deleteSelection}")));
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Alt>Return"), NamedAction.New($"{group}.{showTrack}")));        
        controller.AddShortcut(Shortcut.New(ShortcutTrigger.ParseString("<Control>Return"), NamedAction.New($"{group}.{showAlbum}")));
        AddController(controller);

        var playlistMenu = Menu.NewWithProperties([]);
        playlistMenu.AppendItem(MenuItem.New("Save As...", $"{AppActions.Queue.Key}.{AppActions.Queue.Save.Action}"));        
        playlistMenu.AppendItem(MenuItem.New("Clear", $"{AppActions.Queue.Key}.{AppActions.Queue.Clear.Action}"));
        
         var trackMenu = Menu.NewWithProperties([]);
         trackMenu.AppendItem(MenuItem.New("Remove", $"{group}.{deleteSelection}"));
         trackMenu.AppendItem(MenuItem.New("Track Details", $"{group}.{showTrack}"));        
         trackMenu.AppendItem(MenuItem.New("Show Album", $"{group}.{showAlbum}"));        
         trackMenu.AppendSection(null, playlistMenu);
         _trackPopoverMenu.SetMenuModel(trackMenu);

         var gridMenu = Menu.NewWithProperties([]);
         gridMenu.AppendSection(null, playlistMenu);
         _queuePopoverMenu.SetMenuModel(gridMenu);         
    }
    
    private void ShowTrackContextMenu(TrackListItem listItem, double x, double y)
    {
        var selected = _selection.GetSelected();
        if (selected == Gtk.Constants.INVALID_LIST_POSITION) return;
        
        var pointInItem = new Graphene.Point { X = (float)x, Y = (float)y };
        
        if (!listItem.ComputePoint(_tracksListView, pointInItem, out var pointInListView))
            return;

        var rect = new Rectangle();
        rect.X = (int)Math.Round(pointInListView.X);
        rect.Y = (int)Math.Round(pointInListView.Y);
        rect.Width = 1;
        rect.Height = 1;
        
        _trackPopoverMenu.SetPointingTo(rect);

        if (!_trackPopoverMenu.Visible)
            _trackPopoverMenu.Popup();        
    }
    
    private void ShowQueueContextMenu(double x, double y)
    {
        var rect = new Rectangle();
        rect.X = (int)Math.Round(x);
        rect.Y = (int)Math.Round(y);
        rect.Width = 1;
        rect.Height = 1;
        
        _queuePopoverMenu.SetPointingTo(rect);

        if (!_queuePopoverMenu.Visible)
            _queuePopoverMenu.Popup();        
    }    
    
    private void TracksListViewOnOnActivate(ListView sender, ListView.ActivateSignalArgs args)
    {
         if (_selection.SelectedItem is not QueueTrackModel selectedModel) return;
        TrackActivated?.Invoke(this, new TrackActivatedEventArgs(selectedModel.Position));
    }
    
    private void TrackGestureLongPressOnOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        if (sender.Widget is not TrackListItem listItem) return;
        var position = listItem.Model!.Position;
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);
                
        ShowTrackContextMenu(listItem, args.X, args.Y);                
    }
    
    private void TrackGestureClickOnOnReleased(GestureClick sender, GestureClick.ReleasedSignalArgs args)
    {
        if (sender.Widget is not TrackListItem listItem) return;
        var position = listItem.Model!.Position;
        if (!_selection.IsSelected(position)) _selection.SelectItem(position, true);        

        var button = sender.GetCurrentButton();
        switch (button)
        {
            case Gdk.Constants.BUTTON_PRIMARY:
                TrackActivated?.Invoke(this, new TrackActivatedEventArgs(position));
                break;
            
            case Gdk.Constants.BUTTON_SECONDARY:
                ShowTrackContextMenu(listItem, args.X, args.Y);                
                break;
        }
    }    
    
    private void GridQueueGestureLongPressOnPressed(GestureLongPress sender, GestureLongPress.PressedSignalArgs args)
    {
        ShowQueueContextMenu(args.X, args.Y);
    }

    private void GridQueueGestureClickOnReleased(GestureClick sender, GestureClick.ReleasedSignalArgs args)
    {
        var button = sender.GetCurrentButton();
        switch (button)
        {
            case Gdk.Constants.BUTTON_SECONDARY:
                ShowQueueContextMenu(args.X, args.Y);
                break;
        }
    }
    
    private void QueueShowTrackActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _listStore.GetObject(selected)!;

        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowTrack.Action}", Variant.NewString(item.TrackId.ToString()));
    }

    private void QueueShowAlbumActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _listStore.GetObject(selected)!;

        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Browser.Key}.{AppActions.Browser.ShowAlbum.Action}", Variant.NewString(item.AlbumId.ToString()));        
    }

    private void QueueDeleteSelectionActionOnOnActivate(SimpleAction sender, SimpleAction.ActivateSignalArgs args)
    {
        var selected = _selection.GetSelected();
        if (selected == GtkConstants.GtkInvalidListPosition) return;
        var item = (QueueTrackModel) _listStore.GetObject(selected)!;
        
        // Proactively remove this track from the list.
        // After the server has confirmed this, it would update the list.
        // If this failed, it would still be in the list and would re-appear.
        _listStore.Remove(selected);
        
        // Just invoke the global action as we have one.
        // There is no need for our queue presenter to handle this
        ActivateAction($"{AppActions.Queue.Key}.{AppActions.Queue.RemoveTrack.Action}", Variant.NewString(item.QueueTrackId.ToString()));
    }
}