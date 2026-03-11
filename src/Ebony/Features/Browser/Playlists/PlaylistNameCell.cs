using System.ComponentModel;
using Adw;
using Ebony.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Ebony.Features.Browser.Playlists;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(PlaylistNameCell)}.ui")]
public partial class PlaylistNameCell
{
    [Connect("title-label")] private Label _titleLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("cover-picture")] private Picture _coverPicture;
    
    [Connect("gesture-click")] GestureClick _gestureClick;
    [Connect("gesture-long-press")] GestureLongPress _gestureLongPress;
    [Connect("drag-source")] private DragSource _dragSource;
    
    public GestureClick GestureClick => _gestureClick;
    public GestureLongPress GestureLongPress => _gestureLongPress;

    public PlaylistModel? Model { get; private set; }    
    
    partial void Initialize()
    {
        _dragSource.OnDragBegin += DragOnOnDragBegin;
        _dragSource.OnPrepare += DragOnPrepare;
    }
    
    public void Bind(PlaylistModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        if (Model != null)
        {
            Unbind();
        }
        
        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;
        
        _titleLabel.Label_ = model.Name;
        _subTitleLabel.Label_ = model.Credits;
        
        UpdateCoverPicture();
        
    }

    public void Unbind()
    {
        _coverPicture.SetPaintable(null);            
        Model?.PropertyChanged -= ModelOnPropertyChanged;
        Model = null;
    }
    
    private async void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName != nameof(PlaylistModel.CoverArt)) return;            
            await GtkDispatch.InvokeIdleAsync(UpdateCoverPicture);
        }
        catch (Exception)
        {
            // TODO: Log
        }
    }

    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model.CoverArt?.Paintable);
    }    
    
    private static void DragOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        var widget = (PlaylistNameCell)sender.GetWidget()!;
        var cover = widget.Model!.CoverArt;
        if (cover == null) return;

        var coverPicture = Picture.NewForPaintable(cover.Paintable);
        coverPicture.AddCssClass("cover");
        coverPicture.CanShrink = true;
        coverPicture.ContentFit = ContentFit.ScaleDown;
        coverPicture.AlternativeText = widget.Model.Name;

        var clamp = Clamp.New();
        clamp.MaximumSize = 96;
        clamp.SetChild(coverPicture);

        var dragIcon = DragIcon.GetForDrag(args.Drag);
        dragIcon.SetChild(clamp);
    }
    
    private static ContentProvider? DragOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = sender.GetWidget();
        if (widget is not PlaylistNameCell cell) return null;
        
        var wrapper = GId.NewForId(cell.Model!.PlaylistId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }    
}