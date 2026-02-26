using Aria.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Aria.Features.Browser.Search;

public partial class SearchPage
{
    // DragDrop
    private readonly List<DragSource> _trackDragSources = [];
    private readonly List<DragSource> _albumDragSources = [];
    
    private void ClearDragDrop()
    {
        foreach (var dragSource in _albumDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnOnDragBegin;
            dragSource.OnPrepare -= AlbumDragOnPrepare;            
        }
        _albumDragSources.Clear();
        
        foreach (var dragSource in _trackDragSources)
        {
            dragSource.OnDragBegin -= AlbumOnOnDragBegin;
            dragSource.OnPrepare -= TrackOnPrepare;            
        }
        _albumDragSources.Clear();
    }    
    
    private static ContentProvider AlbumDragOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (SearchAlbumActionRow)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.AlbumId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
    
    private static ContentProvider TrackOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (SearchTrackActionRow)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.TrackId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
    
    private static void AlbumOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
    }    
}