using Ebony.Core.Library;

namespace Ebony.Infrastructure;

public static class CreditsTools
{
    /// <summary>
    /// Gets the set of artists that appear on all tracks in the given list.
    /// </summary>
    public static IEnumerable<TrackArtistInfo> GetCommonArtistsAcrossTracks(IReadOnlyList<AlbumTrackInfo> tracks)
    {
        if (tracks.Count == 0) return [];

        var perTrackArtists = tracks
            .Select(t => t.Track.CreditsInfo.Artists)
            .ToList();

        // Find ids that exist on every track
        var commonIds = perTrackArtists
            .Select(a => a.Select(x => x.Artist.Id).ToHashSet())
            .Aggregate((acc, current) =>
            {
                acc.IntersectWith(current);
                return acc;
            });

        if (commonIds.Count == 0) return [];

        var mergedArtists = commonIds
            .Select(id =>
            {
                var occurrences = perTrackArtists
                    .SelectMany(list => list.Where(a => Equals(a.Artist.Id, id)))
                    .ToList();

                var featured = occurrences.FirstOrDefault(o => o.IsFeatured);

                var mergedRoles = occurrences.Aggregate(ArtistRoles.None, (acc, o) => acc | o.Roles);

                var additionalInfo =
                    featured?.AdditionalInformation
                    ?? occurrences.Select(o => o.AdditionalInformation)
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

                return new TrackArtistInfo
                {
                    Artist = featured?.Artist ?? occurrences[0].Artist,
                    Roles = mergedRoles,
                    AdditionalInformation = additionalInfo,
                    IsFeatured = occurrences.Any(o => o.IsFeatured)
                };
            })
            .OrderBy(a => a.Artist.Name);

        return mergedArtists;
    }

    /// <summary>
    /// Gets the set of artists that are specific to a single track,
    /// i.e., artists who do not appear on all tracks in the album.
    /// </summary>
    public static IEnumerable<TrackArtistInfo> GetTrackSpecificArtists(TrackInfo trackInfo,
        IReadOnlyList<AlbumTrackInfo> albumTracks)
    {
        var sharedGuestArtists = GetCommonArtistsAcrossTracks(albumTracks);

        return trackInfo.CreditsInfo.Artists
            .ExceptBy(sharedGuestArtists.Select(s => s.Artist.Id), a => a.Artist.Id);
    }
}