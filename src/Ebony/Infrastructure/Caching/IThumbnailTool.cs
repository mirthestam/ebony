namespace Ebony.Infrastructure.Caching;

public interface IThumbnailTool
{
    public bool TryCreateThumbnailPng(byte[] inputBytes, int maxWidth, int maxHeight, string fileName);
}