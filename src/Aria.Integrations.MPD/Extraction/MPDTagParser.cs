using Aria.Core.Extraction;
using Aria.Core.Library;

namespace Aria.Backends.MPD.Extraction;

/// <summary>
/// Specific MPD tag parser that user the underlying parsing strategy to parse the tags
/// </summary>
public class MPDTagParser(ITagParser tagParser)
{
    public IEnumerable<AlbumInfo> ParseAlbums(IReadOnlyList<Tag> tags)
    {
        // We start by processing all tags that make AlbumTracks.
        // After that, we consolidate by merging tracks with the same Album ID into one album
        
        var parsedInfo = new List<(AlbumInfo Album, AlbumTrackInfo Track)>();
        var currentTags = new List<Tag>();

        foreach (var tag in tags)
        {
            // If we encounter a new 'file', and we already have data, parse the previous track
            if (tag.Name.Equals(MPDTagNames.FileTags.File, StringComparison.OrdinalIgnoreCase) && currentTags.Count > 0)
            {
                parsedInfo.Add(ParseAlbumInformation(currentTags));
                currentTags.Clear();
            }

            currentTags.Add(tag);
        }
        
        if (currentTags.Count > 0)
        {
            parsedInfo.Add(ParseAlbumInformation(currentTags));
        }
        
        // Group by Album ID and consolidate all individual tracks into full AlbumInfo objects
        var albums =  parsedInfo
            .GroupBy(info => info.Album.Id)
            .Select(albumGroup =>
            {
                var referenceAlbum = albumGroup.First().Album;

                // Collect and deduplicate all tracks found for this album
                var albumTracks = albumGroup
                    .Select(x => x.Track)
                    .DistinctBy(t => t.Track.Id)
                    .ToList();

                // Merge all credits found across all tracks in this group
                var albumArtists = albumGroup
                    .SelectMany(x => x.Album.CreditsInfo.AlbumArtists)
                    .GroupBy(a => a.Artist.Id)
                    .Select(g =>
                    {
                        var referenceAlbumArtist = g.First();

                        // Found roles can be different on each track.
                        // Merge them for album artists
                        var result = g.Aggregate(default(ArtistRoles), (current, info) => current | info.Roles);
                        return new AlbumArtistInfo
                        {
                            Artist = referenceAlbumArtist.Artist,
                            Roles = result
                        };
                    })
                    .ToList();

                var artists = albumGroup.SelectMany(x => x.Album.CreditsInfo.Artists)
                    .DistinctBy(a => a.Artist.Id)
                    .ToList();
                
                var assetsReferenceTrack = albumTracks.First();
                
                return referenceAlbum with
                {
                    Assets = assetsReferenceTrack.Track.Assets,
                    Tracks = albumTracks,
                    CreditsInfo = referenceAlbum.CreditsInfo with
                    {
                        AlbumArtists = albumArtists,
                        Artists = artists
                    }
                };
            });

        return albums;
    }

    public IEnumerable<QueueTrackInfo> ParseQueue(IReadOnlyList<Tag> tags)
    {
        var currentTrackTags = new List<Tag>();
        foreach (var tag in tags)
        {
            // Each 'file' key marks the start of a new track in the response stream
            if (tag.Name.Equals(MPDTagNames.FileTags.File, StringComparison.OrdinalIgnoreCase) && currentTrackTags.Count > 0)
            {
                var track = tagParser.ParseQueueTrack(currentTrackTags);
                
                // TODO: adding this track asset is duplicatead in a few places.
                // However, it is MPD specific, so DRY.
                if (track.Track.FileName != null)
                {
                    // This logic is duplicate with logic in the library.
                    track = track with
                    {
                        Track = track.Track with
                        {
                            Assets =
                            [
                                new AssetInfo
                                {
                                    Id = new AssetId(track.Track.FileName),
                                    Type = AssetType.FrontCover
                                }
                            ]
                        }
                    };
                }
                yield return track;
                currentTrackTags.Clear();
            }
                
            currentTrackTags.Add(tag);
        }

        if (currentTrackTags.Count <= 0) yield break;
        {
            var track = tagParser.ParseQueueTrack(currentTrackTags);
            yield return track;
            currentTrackTags.Clear();
        }
    }    
    
    public IEnumerable<AlbumTrackInfo> ParsePlaylist(IReadOnlyList<Tag> tags)
    {
        var parsedResults = new List<AlbumTrackInfo>();
        var currentTrackTags = new List<Tag>();

        foreach (var tag in tags)
        {
            // If we encounter a new 'file', and we already have data, parse the previous track
            if (tag.Name.Equals(MPDTagNames.FileTags.File, StringComparison.OrdinalIgnoreCase) && currentTrackTags.Count > 0)
            {
                // This tag indicates a new track.

                // Store our AlbumInfo and TrackInfo pair
                var albumInformation = ParseAlbumInformation(currentTrackTags);                
                parsedResults.Add(albumInformation.Track);
                currentTrackTags.Clear();
            }

            currentTrackTags.Add(tag);
        }
        
        if (currentTrackTags.Count > 0)
        {
            var albumInformation = ParseAlbumInformation(currentTrackTags); 
            parsedResults.Add(albumInformation.Track);
        }

        return parsedResults;
    }    
    
    private (AlbumInfo Album, AlbumTrackInfo Track) ParseAlbumInformation(List<Tag> trackTags)
    {
        // First we parse the track using the actual tag parser.
        // Then we parse album information from these tags.
        var albumTrack = tagParser.ParseAlbumTrack(trackTags);
        
        if (albumTrack.Track.FileName != null)
        {
            // For MPD we want to look up album art by filename
            albumTrack = albumTrack with
            {
                Track = albumTrack.Track with
                {
                    Assets =
                    [
                        new AssetInfo
                        {
                            Id = new AssetId(albumTrack.Track.FileName),
                            Type = AssetType.FrontCover
                        }
                    ]
                }
            };
        }
        
        var album = tagParser.ParseAlbum(trackTags);

        return (album, albumTrack);
    }
}