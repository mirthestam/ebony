using Aria.Core.Extraction;

namespace Aria.Infrastructure.Caching;

public interface IAlbumArtCache
{
    public Stream? GetAlbumResourceStream(Id resourceId);

    public Task<Stream?> CreateThumbnailAndCacheStream(Id resourceId, Stream sourceStream, CancellationToken cancellationToken = default);

    public bool Contains(Id resourceId);
    
    public void MarkFailed(Id resourceId);
}