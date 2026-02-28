using GdkPixbuf;

namespace Aria.Infrastructure.Caching;

public class ThumbnailTool : IThumbnailTool
{
    public bool TryCreateThumbnailPng(byte[] inputBytes, int maxWidth, int maxHeight, string fileName)
    {
        try
        {
            using var loader = PixbufLoader.NewWithProperties([]);
                
            loader.Write(inputBytes);
            loader.Close();
            
            var pixelBuffer = loader.GetPixbuf();
            if (pixelBuffer == null) return false;
            
            if (pixelBuffer.Width <= 0 || pixelBuffer.Height <= 0) return false;
            
            var scaledPixelBuffer = pixelBuffer.ScaleSimple(maxWidth, maxHeight, InterpType.Bilinear);
            return scaledPixelBuffer != null && scaledPixelBuffer.Savev(fileName, "png", [], []);
        }
        catch
        {
            return false;
        }
    }    
}