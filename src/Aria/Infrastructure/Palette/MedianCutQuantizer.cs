internal static class MedianCutQuantizer
{
    private const int SIGBITS = 5;
    private const int RSHIFT = 8 - SIGBITS;
    private const int HISTOSIZE = 1 << (SIGBITS * 3);

    public static List<Rgb> Quantize(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int rowstride,
        int channels,
        int maxColors)
    {
        Span<int> hist = stackalloc int[HISTOSIZE];

        // --- HISTOGRAM PASS (single scan) ---
        for (var y = 0; y < height; y++)
        {
            var row = pixels[(y * rowstride)..];

            for (var x = 0; x < width; x++)
            {
                var i = x * channels;

                var r = row[i];
                var g = row[i + 1];
                var b = row[i + 2];

                var idx = GetColorIndex(r, g, b);
                hist[idx]++;
            }
        }

        // --- INITIAL VBOX ---
        var vbox = VBox.Create(hist);
        var pq = new PriorityQueue<VBox, int>();
        pq.Enqueue(vbox, -vbox.Count);

        // --- SPLIT ---
        while (pq.Count < maxColors)
        {
            if (!pq.TryDequeue(out var box, out _))
                break;

            if (box.Count == 0)
                break;

            var (a, b) = box.Split(hist);
            pq.Enqueue(a, -a.Count);
            pq.Enqueue(b, -b.Count);
        }

        // --- AVERAGE COLORS ---
        var result = new List<Rgb>(pq.Count);
        foreach (var vb in pq.UnorderedItems)
            result.Add(vb.Element.Average(hist));

        return result;
    }

    private static int GetColorIndex(byte r, byte g, byte b)
        => ((r >> RSHIFT) << (2 * SIGBITS)) |
           ((g >> RSHIFT) << SIGBITS) |
           (b >> RSHIFT);
}