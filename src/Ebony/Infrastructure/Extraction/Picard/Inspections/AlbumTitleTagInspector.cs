using Ebony.Core.Extraction;
using Ebony.Infrastructure.Inspection;

namespace Ebony.Infrastructure.Extraction.Picard.Inspections;

public class AlbumTitleTagInspector : TagInspector
{
    public override IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags)
    {
        var albumArtists = tags.Where(t => t.Name.Equals(PicardTags.AlbumTags.Album, StringComparison.OrdinalIgnoreCase));
        if (!albumArtists.Any())
        {
            yield return new Diagnostic(
                Severity.Problem,
                "Album name is required.",
                "Album name is essential for proper organization and grouping of tracks within an album. It enables Ebony to correctly identify and display all tracks from the same album together, even when individual track artists differ."
            );
        }
    }    
}