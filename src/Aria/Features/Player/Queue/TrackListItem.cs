using System.ComponentModel;
using Aria.Core.Player;
using Aria.Infrastructure;
using GObject;
using Gtk;

namespace Aria.Features.Player.Queue;

[Subclass<Box>]
[Template<AssemblyResource>("Aria.Features.Player.Queue.TrackListItem.ui")]
public partial class TrackListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("composer-label")] private Label _composerLabel;
    [Connect("duration-label")] private Label _durationLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
    // [Connect("playing-picture")] private Picture _playingPicture;
    
    [Connect("gesture-click")] GestureClick _gestureClick;
    [Connect("gesture-long-press")] GestureLongPress _gestureLongPress;
    
    public GestureClick GestureClick => _gestureClick;
    public GestureLongPress GestureLongPress => _gestureLongPress;
    
    public QueueTrackModel? Model { get; private set; }
    
    public void Bind(QueueTrackModel model)
    {
        if (Model != null)
        {
            // ListItems can be reused by GTK for different models.
            _coverPicture.SetPaintable(null);
            Model.PropertyChanged -= ModelOnPropertyChanged;
        }

        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;

        _titleLabel.SetLabel(model.TitleText);
        _subTitleLabel.SetLabel(model.SubTitleText);
        _composerLabel.SetLabel(model.ComposersText);
        _subTitleLabel.Visible = !string.IsNullOrEmpty(model.SubTitleText);
        _composerLabel.Visible = !string.IsNullOrEmpty(model.ComposersText);
        _durationLabel.SetLabel(model.DurationText);

        UpdateCoverPicture();
    }

    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverArt?.Paintable);
    }

    private void UpdatePlaying()
    {
        var parent = GetParent();


        if (Model?.Playing is null or PlaybackState.Unknown)
        {
            //_playingPicture.Visible = false;
            parent?.RemoveCssClass("playing");
            return;
        }
        
        parent?.AddCssClass("playing");
        //_playingPicture.Visible = true;
    }

    private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QueueTrackModel.Playing))
        {
            GtkDispatch.InvokeIdle(UpdatePlaying);
        }

        if (e.PropertyName != nameof(QueueTrackModel.CoverArt)) return;
        GtkDispatch.InvokeIdle(UpdateCoverPicture);
    }
}