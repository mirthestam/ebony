using Ebony.Core.Extraction;
using Ebony.Core.Library;
using Ebony.Infrastructure.Extraction.Picard.Inspections;
using Ebony.Infrastructure.Inspection;

namespace Ebony.Infrastructure.Extraction;

public class PicardTagInspector(ITagParser parser) : ITagInspector
{
    // Inspection
    private readonly IReadOnlyList<TagInspector> _tagInspectors =
    [
        new AlbumTitleTagInspector(),
        new AlbumTrackTagInspector(),
        new AlbumArtistTagInspector(),
        new AlbumReleasedTagInspector(),
        new WorkTagInspector()
    ];
    
    private readonly IReadOnlyList<AlbumInspector> _albumInspectors =
    [
        new AlbumArtistsInspector()
    ];
    
    public Inspected<AlbumInfo> InspectAlbum(IReadOnlyList<Tag> sourceTags)
    {
        var album = parser.ParseAlbum(sourceTags);
        var diagnostics = Inspect(sourceTags).ToList();
        diagnostics.AddRange(Inspect(album));
        
        return new Inspected<AlbumInfo>(album, diagnostics);
    }
    
    private IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags) => _tagInspectors.SelectMany(i => i.Inspect(tags));
    private IEnumerable<Diagnostic> Inspect(AlbumInfo album) => _albumInspectors.SelectMany(i => i.Inspect(album));
}