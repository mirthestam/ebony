using Ebony.Core.Extraction;
using Ebony.Core.Library;

namespace Ebony.Infrastructure.Inspection;

public interface ITagInspector
{
    public Inspected<AlbumInfo> InspectAlbum(IReadOnlyList<Tag> sourceTags);
}