using Ebony.Core.Extraction;
using Ebony.Core.Library;

namespace Ebony.Infrastructure.Extraction;

public partial class PicardTagParser
{
    // Queue
    public QueueTrackInfo ParseQueueTrack(IReadOnlyList<Tag> sourceTags)
    {
        var tags = ParseTags(sourceTags);
        
        if (tags.QueuePosition is null) throw new InvalidOperationException("Position tag is missing");

        var track = ParseTrack(tags);
        
        var queueTrack = new QueueTrackInfo
        {
            Id = Id.Undetermined,
            Position = tags.QueuePosition.Value,
            Track = track
        };

        var queueTrackId = idProvider.CreateQueueTrackId(new QueueTrackBaseIdentificationContext
        {
            Tags = sourceTags
        });

        return queueTrack with { Id = queueTrackId };
    }    
    
    private TrackInfo ParseTrack(PicardTagInfo tags)
    {
        var credits = ParseTrackCredits(tags);

        var track = new TrackInfo
        {
            FileName = tags.FileName,
            CreditsInfo = credits,
            Work = new WorkInfo
            {
                Work = tags.Work,
                MovementName = tags.MovementName,
                MovementNumber = tags.MovementNumber,
                ShowMovement = tags.ShowMovement
            },
            Title = tags.Title,
            Duration = tags.Duration,
            ReleaseDate = DateTagParser.ParseDate(tags.Date),
            AlbumId = Id.Undetermined,
            Id = Id.Undetermined
        };

        var trackId = idProvider.CreateTrackId(new TrackBaseIdentificationContext { Track = track });
        track = track with { Id = trackId };

        var album = ParseAlbum(tags, track.CreditsInfo);
        return track with { AlbumId = album.Id };
    }
}