using Ebony.Core.Extraction;
using Ebony.Infrastructure.Inspection;

namespace Ebony.Infrastructure.Extraction.Picard.Inspections;

public class AlbumArtistTagInspector : TagInspector
{
    public override IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags)
    {
        var albumArtists = tags.Where(t => t.Name.Equals(PicardTags.AlbumTags.AlbumArtist, StringComparison.OrdinalIgnoreCase));
        if (!albumArtists.Any())
        {
            yield return new Diagnostic(
                Severity.Problem,
                "Album artist is required.",
                "Album artist is essential for proper organization and grouping of tracks within an album. It enables Ebony to correctly identify and display all tracks from the same album together, even when individual track artists differ."
            );
        }
    }
}

public class AlbumReleasedTagInspector : TagInspector
{
    public override IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags)
    {
        var dateTags = tags.Where(t => t.Name.Equals(PicardTags.TrackTags.Date, StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (dateTags.Count == 0)
        {
            yield return new Diagnostic(
                Severity.Problem,
                "Release date is required.",
                "Release date is essential to identify unique albums and for chronological sorting of albums. It should be specified as either a year (YYYY) or a full date (YYYY-MM-DD)."
            );
            yield break;
        }

        foreach (var value in dateTags.Select(dateTag => dateTag.Value.Trim()))
        {
            // Check if it's a valid year (YYYY)
            if (value.Length == 4 && int.TryParse(value, out var year) && year >= 1000 && year <= 9999)
                continue;

            if (DateTime.TryParse(value, out _))
                continue;
            
            yield return new Diagnostic(
                Severity.Problem,
                "Release date has invalid format.",
                $"Release date must be either a valid year (YYYY) or a full date in YYYY-MM-DD format. Current value: '{value}'."
            );
        }
    }
}