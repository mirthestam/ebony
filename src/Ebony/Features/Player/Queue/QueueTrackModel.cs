using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Core.Player;
using Ebony.Features.Browser.Album;
using Ebony.Infrastructure;
using GObject;
using Object = GObject.Object;

namespace Ebony.Features.Player.Queue;

[Subclass<Object>]
public partial class QueueTrackModel : INotifyPropertyChanged
{
    public static QueueTrackModel NewFromQueueTrackInfo(QueueTrackInfo queueTrack, QueueModel queueModel)
    {
        var model = NewWithProperties([]);
        model.Queue = queueModel;
        model.Parse(queueTrack);
        
        return model;
    }

    private void Parse(QueueTrackInfo queueTrack)
    {
        var track = queueTrack.Track;
        TitleText = track.Title;
        if (track.Work?.ShowMovement ?? false)
            // For  these kind of works, we ignore the
            TitleText = $"{track.Work.MovementName} ({track.Work.MovementNumber} {track.Title} ({track.Work.Work})";
        
        var credits = track.CreditsInfo;
        var artists = string.Join(", ", credits.OtherArtists.Select(x => x.Artist.Name));
    
        var details = new List<string>();
        var conductors = string.Join(", ", credits.Conductors.Select(x => x.Artist.Name));
        if (!string.IsNullOrEmpty(conductors))
            details.Add($"{conductors}");
    
        ComposersText = string.Join(", ", credits.Composers.Select(x => x.Artist.Name));
    
        SubTitleText = artists;
        if (details.Count > 0) SubTitleText += $" ({string.Join(", ", details)})";
    
        
        if (queueTrack.Track.Duration == TimeSpan.Zero)
        {
            DurationText = "—:—";
        }
        else
        {
            DurationText = queueTrack.Track.Duration.ToDisplayString();
        }
        
        QueueTrackId = queueTrack.Id;
        AlbumId = queueTrack.Track.AlbumId;
        TrackId = queueTrack.Track.Id;
        Position = queueTrack.Position;    
    }

    public QueueModel Queue { get; set; }

    public uint Position { get; set; }
    
    public Id QueueTrackId { get; set; }
    
    public Id AlbumId { get; set; }
    
    public Id TrackId { get; set; }
    
    public string TitleText { get; set; }
    
    public string SubTitleText { get; set; }
    
    public string ComposersText { get; set; }
    
    public string DurationText { get; set; }

    public PlaybackState Playing
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
        }        
    }
    
    public Art? CoverArt
    {
        get;
        set
        {
            if (ReferenceEquals(field, value)) return;

            // This model owns the texture once assigned.
            field?.Dispose();

            field = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}