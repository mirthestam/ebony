using Ebony.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Ebony.Features.Player.Queue;

public partial class Queue
{
    private void SetupDragDropForItem(TrackListItem child)
    {
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Move;
        dragSource.OnPrepare += TrackOnDragPrepare;
        child.AddController(dragSource);

        // Drag targets
        var type = GObject.Type.Object;

        var idWrapperDropTarget = DropTarget.New(type, DragAction.Copy);
        idWrapperDropTarget.OnDrop += TrackOnGIdDropped;
        child.AddController(idWrapperDropTarget);

        var playlistPositionDropTarget = DropTarget.New(type, DragAction.Move);
        playlistPositionDropTarget.OnDrop += TrackOnPlaylistPositionDropped;
        child.AddController(playlistPositionDropTarget);
    }    
    
    private bool TrackOnGIdDropped(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something on a track in this playlist.
        var value = args.Value.GetObject();

        if (value is not GId gId) return false;

        var widget = (TrackListItem)sender.Widget!;
        EnqueueRequested(this, new EnqueueRequestedEventArgs(gId.Id, widget.Model!.Position));

        return true;
    }
    
    private bool TrackOnPlaylistPositionDropped(DropTarget sender, DropTarget.DropSignalArgs args)
    {
        // The user 'dropped' something on a track in this playlist.
        var value = args.Value.GetObject();

        if (value is not GQueueTrackId queueTrackId) return false;
        var widget = (TrackListItem)sender.Widget!;

        MoveRequested(this, new MoveRequestedEventArgs(queueTrackId.Id, widget.Model!.Position));
        return true;
    }    

    private static ContentProvider TrackOnDragPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (TrackListItem)sender.GetWidget()!;
        var data = GQueueTrackId.NewWithId(widget.Model!.QueueTrackId);
        var value = new Value(data);
        return ContentProvider.NewForValue(value);
    }
}