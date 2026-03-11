using System.Globalization;
using Ebony.Core.Extraction;
using Ebony.Core.Library;

namespace Ebony.Infrastructure.Extraction;

public partial class PicardTagParser
{
    // Album
    public AlbumInfo ParseAlbum(IReadOnlyList<Tag> sourceTags)
    {
        var tags = ParseTags(sourceTags);
        var credits = ParseTrackCredits(tags);

        return ParseAlbum(tags, credits);
    }
    
    public AlbumTrackInfo ParseAlbumTrack(IReadOnlyList<Tag> sourceTags)
    {
        var tags = ParseTags(sourceTags);
        var track = ParseTrack(tags);

        int? trackNumber = null;
        if (int.TryParse(tags.TrackNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tn))
            trackNumber = tn;

        TrackGroup? group = null;

        if (!string.IsNullOrWhiteSpace(tags.Heading))
        {
            group = new TrackGroup
            {
                Header = tags.Heading,
                Key = tags.Heading
            };
        }
        
        return new AlbumTrackInfo
        {
            TrackNumber = trackNumber,
            VolumeName = tags.Disc,
            Track = track,
            Group = group,
            Id = track.Id
        };
    }

    private AlbumInfo ParseAlbum(PicardTagInfo tags, TrackCreditsInfo credits)
    {
        var releaseDate = DateTagParser.ParseDate(tags.Date);

        var album = new AlbumInfo
        {
            Title = tags.AlbumTitle,
            CreditsInfo = new AlbumCreditsInfo
            {
                Artists = credits.Artists,
                AlbumArtists = credits.AlbumArtists
            },
            ReleaseDate = releaseDate,
            Id = Id.Undetermined
        };

        var id = idProvider.CreateAlbumId(new AlbumBaseIdentificationContext { Album = album });
        return album with { Id = id };
    }
}