using Gdk;
using GdkPixbuf;

public static class PaletteExtractor
{
    public static RGBA[]? LoadPalette(Pixbuf pixelBuffer, int colorCount = 5)
    {
        // Span over de hele buffer
        var pixels = pixelBuffer.PixelBytes.GetRegionSpan<byte>(0, pixelBuffer.GetByteLength());

        var quantized = MedianCutQuantizer.Quantize(
            pixels,
            pixelBuffer.Width,
            pixelBuffer.Height,
            pixelBuffer.Rowstride,
            pixelBuffer.NChannels,
            colorCount);

        if (quantized.Count == 0)
            return null;

        var result = new List<RGBA>(quantized.Count);

        foreach (var c in quantized)
        {
            var r = new RGBA();
            r.Red = c.R / 255f;
            r.Green = c.G / 255f;         
            r.Blue = c.B / 255f;
            r.Alpha = 1f;
            result.Add(r);
        }

        return result.ToArray();
    }
}