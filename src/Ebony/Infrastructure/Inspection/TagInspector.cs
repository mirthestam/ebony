using Ebony.Core.Extraction;
using Ebony.Core.Library;

namespace Ebony.Infrastructure.Inspection;

public abstract class TagInspector
{
    public abstract IEnumerable<Diagnostic> Inspect(IReadOnlyList<Tag> tags);
}

public abstract class AlbumInspector
{
    public abstract IEnumerable<Diagnostic> Inspect(AlbumInfo album);    
}