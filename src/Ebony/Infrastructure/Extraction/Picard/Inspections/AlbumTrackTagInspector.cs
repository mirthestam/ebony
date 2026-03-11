using Ebony.Core.Extraction;
using Ebony.Infrastructure.Inspection;

namespace Ebony.Infrastructure.Extraction.Picard.Inspections;

public class AlbumTrackTagInspector : TagInspector
{
    public override IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags)
    {
        var albumArtists = tags.Where(t => t.Name.Equals(PicardTags.AlbumTags.Track, StringComparison.OrdinalIgnoreCase));
        if (!albumArtists.Any())
        {
            yield return new Diagnostic(
                Severity.Warning,
                "Track number is missing.",
                "Track number helps maintain proper ordering of songs within an album. Without it, tracks may appear in incorrect sequence, making it difficult to experience the album as intended by the artist."
            );
        }
    }    
}