using System.Text.RegularExpressions;
using Ebony.Core.Extraction;
using Ebony.Core.Library;

namespace Ebony.Infrastructure.Extraction;

public partial class PicardTagParser
{
    // Artists
    private sealed record ParsedArtistName(string Name, string? Extra);

    private static readonly Regex PerformerSuffixRegex = PerformerSuffixRegexFactory();

    public ArtistInfo ParseArtist(string artistName, string? artistNameSort, ArtistRoles roles)
    {
        var artistNameParts = ParseArtistNameParts(artistName);
        var artistNameSortParts = ParseArtistNameParts(artistNameSort);

        return new ArtistInfo
        {
            Name = artistNameParts?.Name ?? artistName,
            NameSort = artistNameSortParts?.Extra ?? artistNameSort,
            Roles = roles,
            Id = Id.Undetermined
        };
    }

    private TrackCreditsInfo ParseTrackCredits(PicardTagInfo tags)
    {
        var artists = new List<TrackArtistInfo>();
        var albumArtists = new List<TrackArtistInfo>();

        foreach (var s in tags.AlbumArtistTags.Where(s => !string.IsNullOrWhiteSpace(s)))
            AddArtist(s, ArtistRoles.None, true);
        
        foreach (var s in tags.ArtistTags.Where(s => !string.IsNullOrWhiteSpace(s)))
            AddArtist(s, ArtistRoles.Unknown);
        
        foreach (var s in tags.ComposerTags.Where(s => !string.IsNullOrWhiteSpace(s)))
            AddArtist(s, ArtistRoles.Composer);

        foreach (var s in tags.PerformerTags.Where(s => !string.IsNullOrWhiteSpace(s)))
            AddArtist(s, ArtistRoles.Performer);

        foreach (var s in tags.EnsembleTags.Where(s => !string.IsNullOrWhiteSpace(s)))
            AddArtist(s, ArtistRoles.Ensemble);

        if (!string.IsNullOrWhiteSpace(tags.Conductor))
            AddArtist(tags.Conductor, ArtistRoles.Conductor);
        
        return new TrackCreditsInfo
        {
            Artists = artists.Where(a => a.Roles != ArtistRoles.None).ToList(),
            AlbumArtists = artists.Where(a => a.IsFeatured).Select(a => new AlbumArtistInfo
            {
                Artist = a.Artist,
                Roles = a.Roles
            }).ToList()
        };

        void AddArtist(string artistName, ArtistRoles roles, bool isFeatured = false)
        {
            var parts = ParseArtistNameParts(artistName);

            var existingArtist = artists.FirstOrDefault(a => a.Artist.Name == parts?.Name);
            if (existingArtist != null)
            {
                // This artist is already known. Add the role
                var index = artists.IndexOf(existingArtist);
                artists[index] = existingArtist with
                {
                    Roles = existingArtist.Roles | roles,
                    IsFeatured = existingArtist.IsFeatured || isFeatured
                };
                return;
            }

            var artistInfo = new TrackArtistInfo
            {
                Roles = roles,
                Artist = new ArtistInfo
                {
                    Name = parts?.Name ?? artistName,
                    Id = Id.Empty
                },
                AdditionalInformation = parts?.Extra,
                IsFeatured = isFeatured
            };

            var artistId = idProvider.CreateArtistId(new ArtistBaseIdentificationContext
                { Artist = artistInfo.Artist });
            artistInfo = artistInfo with { Artist = artistInfo.Artist with { Id = artistId } };

            artists.Add(artistInfo);
        }
    }

    private static ParsedArtistName? ParseArtistNameParts(string? value)
    {
        // Picard uses 'Name (Extra)' format for artists.'
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        var match = PerformerSuffixRegex.Match(trimmed);
        if (!match.Success)
            return new ParsedArtistName(trimmed, null);

        var name = match.Groups["name"].Value.Trim();
        var extra = match.Groups["extra"].Value.Trim();

        return string.IsNullOrWhiteSpace(name)
            ? new ParsedArtistName(trimmed, null)
            : new ParsedArtistName(name, string.IsNullOrWhiteSpace(extra) ? null : extra);
    }

    [GeneratedRegex(@"^(?<name>.*?)\s*\((?<extra>[^()]+)\)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PerformerSuffixRegexFactory();
}