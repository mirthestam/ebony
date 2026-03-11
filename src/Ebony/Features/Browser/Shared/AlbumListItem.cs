using System.ComponentModel;
using Adw;
using Ebony.Infrastructure;
using Gdk;
using GObject;
using Gtk;

namespace Ebony.Features.Browser.Shared;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(AlbumListItem)}.ui")]
public partial class AlbumListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;

    [Connect("gesture-click")] private GestureClick _gestureClick;
    [Connect("gesture-long-press")] private GestureLongPress _gestureLongPress;
    [Connect("drag-source")] private DragSource _dragSource;

    public GestureClick GestureClick => _gestureClick;
    public GestureLongPress GestureLongPress => _gestureLongPress;

    public AlbumModel? Model { get; private set; }

    partial void Initialize()
    {
        _dragSource.OnDragBegin += DragOnOnDragBegin;
        _dragSource.OnPrepare += DragOnPrepare;
    }

    public void Bind(AlbumModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (Model != null)
        {
            Unbind();
        }

        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        _titleLabel.SetLabel(model.Title);
        _subTitleLabel.SetLabel(model.Credits);

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
            if (e.PropertyName != nameof(AlbumModel.CoverArt)) return;
            await GtkDispatch.InvokeIdleAsync(UpdateCoverPicture);
        }
        catch (Exception)
        {
            // TODO: Log
        }
    }

    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverArt?.Paintable);
    }

    private static void DragOnOnDragBegin(DragSource sender, DragSource.DragBeginSignalArgs args)
    {
        var widget = (AlbumListItem)sender.GetWidget()!;
        var cover = widget.Model!.CoverArt;
        if (cover == null) return;

        var coverPicture = Picture.NewForPaintable(cover.Paintable);
        coverPicture.AddCssClass("cover");
        coverPicture.CanShrink = true;
        coverPicture.ContentFit = ContentFit.ScaleDown;
        coverPicture.AlternativeText = widget.Model.Title;

        var clamp = Clamp.New();
        clamp.MaximumSize = 96;
        clamp.SetChild(coverPicture);

        var dragIcon = DragIcon.GetForDrag(args.Drag);
        dragIcon.SetChild(clamp);
    }

    private static ContentProvider DragOnPrepare(DragSource sender, DragSource.PrepareSignalArgs args)
    {
        var widget = (AlbumListItem)sender.GetWidget()!;
        var wrapper = GId.NewForId(widget.Model!.AlbumId);
        var value = new Value(wrapper);
        return ContentProvider.NewForValue(value);
    }
}