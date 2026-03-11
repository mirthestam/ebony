using System.ComponentModel;
using Ebony.Core.Player;
using Ebony.Core.Queue;
using Ebony.Infrastructure;
using GObject;
using Gtk;

namespace Ebony.Features.Player.Queue;

[Subclass<Box>]
[Template<AssemblyResource>($"ui/{nameof(TrackListItem)}.ui")]
public partial class TrackListItem
{
    [Connect("cover-picture")] private Picture _coverPicture;
    [Connect("composer-label")] private Label _composerLabel;
    [Connect("duration-label")] private Label _durationLabel;
    [Connect("subtitle-label")] private Label _subTitleLabel;
    [Connect("title-label")] private Label _titleLabel;
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
            Model.Queue.PropertyChanged -= QueueOnPropertyChanged;
        }

        Model = model;
        Model.PropertyChanged += ModelOnPropertyChanged;
        Model.Queue.PropertyChanged += QueueOnPropertyChanged;

        _titleLabel.SetLabel(model.TitleText);
        _subTitleLabel.SetLabel(model.SubTitleText);
        _composerLabel.SetLabel(model.ComposersText);
        _subTitleLabel.Visible = !string.IsNullOrEmpty(model.SubTitleText);
        _composerLabel.Visible = !string.IsNullOrEmpty(model.ComposersText);
        _durationLabel.SetLabel(model.DurationText);

        UpdateCoverArtRevealer();
        UpdateCoverPicture();
    }

    private void UpdateCoverPicture()
    {
        _coverPicture.SetPaintable(Model?.CoverArt?.Paintable);
    }

    private void UpdateCoverArtRevealer()
    {
        if (Model is null) _coverPicture.Visible = false;
        else
            _coverPicture.Visible = Model.Queue.Mode switch
            {
                QueueMode.SingleAlbum => false,
                QueueMode.Playlist => true,
                _ => throw new ArgumentOutOfRangeException()
            };
    }

    private void UpdatePlaying()
    {
        var parent = GetParent();


        if (Model?.Playing is null or PlaybackState.Unknown)
        {
            parent?.RemoveCssClass("playing");
            return;
        }

        parent?.AddCssClass("playing");
    }

    private async void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(QueueTrackModel.Playing))
            {
                await GtkDispatch.InvokeIdleAsync(UpdatePlaying);
            }

            if (e.PropertyName != nameof(QueueTrackModel.CoverArt)) return;
            await GtkDispatch.InvokeIdleAsync(UpdateCoverPicture);
        }
        catch (Exception)
        {
             // TODO: Log
        }

    }

    private async void QueueOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(QueueModel.Mode))
            {
                await GtkDispatch.InvokeIdleAsync(UpdateCoverArtRevealer);
            }
        }
        catch (Exception)
        {
            // TODO: Log
        }
    }
}