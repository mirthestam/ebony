using Ebony.Core.Library;
using Ebony.Infrastructure.Inspection;

namespace Ebony.Infrastructure.Extraction.Picard.Inspections;

public class AlbumArtistsInspector : AlbumInspector
{
    public override IEnumerable<Diagnostic> Inspect(AlbumInfo album)
    {
        if (album.CreditsInfo.Artists.Count == 0)
        {
            yield return new Diagnostic(
                Severity.Info,
                "Only AlbumArtists are present.",
                "There are no TrackArtists present in this album. Roles for artists are defined on Track level. Therefore, artist appear without context."
            );
        }

        if (album.CreditsInfo.AlbumArtists.Count == 0)
        {
            yield return new Diagnostic(
                Severity.Problem,
                "Album Artists are required.",
                "Album artist is essential for proper organization and grouping of tracks within an album. It enables Ebony to correctly identify and display all tracks from the same album together, even when individual track artists differ."
            );
        }
        
        // TODO: Cannot inspect this yet as the calls to this function are with single tracks
        // as things have not been concatinated yet here
        // var trackArtists = album.Tracks
        //     .SelectMany(t => t.Track.CreditsInfo.Artists)
        //     .Select(ca => ca.Artist.Id)
        //     .Distinct().ToList();
        //
        // foreach (var albumArtist in album.CreditsInfo.AlbumArtists)
        // {
        //     if (trackArtists.All(ta => ta != albumArtist.Artist.Id))
        //     {
        //         yield return new Diagnostic(
        //             Severity.Warning,
        //             $"Album artist '{albumArtist.Artist.Name}' is not present on any track.",
        //             "This album artist does not appear as an artist on any individual track. This may indicate incomplete or inconsistent metadata, as album artists typically perform on at least some tracks."
        //         );
        //     }
        // }
    }
}