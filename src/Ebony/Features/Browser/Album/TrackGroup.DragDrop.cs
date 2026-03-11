using Ebony.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Ebony.Features.Browser.Album;

public partial class TrackGroup
{
    private static ContentProvider TrackOnDragPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var row = (AlbumTrackRow)sender.GetWidget()!;
        var wrapper = GId.NewForId(row.TrackId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }
    
    private static void InitializeTrackDragSource(AlbumTrackRow row)
    {
        var dragSource = DragSource.New();
        dragSource.Actions = DragAction.Copy;
        dragSource.OnPrepare += TrackOnDragPrepare;
        row.AddController(dragSource);
    }    
}