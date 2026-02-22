internal sealed class VBox
{
    public int R1, R2, G1, G2, B1, B2;
    public int Count;

    public static VBox Create(Span<int> hist)
    {
        int rmin = 31, rmax = 0;
        int gmin = 31, gmax = 0;
        int bmin = 31, bmax = 0;

        for (int i = 0; i < hist.Length; i++)
        {
            if (hist[i] == 0) continue;

            int r = (i >> 10) & 31;
            int g = (i >> 5) & 31;
            int b = i & 31;

            rmin = Math.Min(rmin, r);
            rmax = Math.Max(rmax, r);
            gmin = Math.Min(gmin, g);
            gmax = Math.Max(gmax, g);
            bmin = Math.Min(bmin, b);
            bmax = Math.Max(bmax, b);
        }

        return new VBox { R1 = rmin, R2 = rmax, G1 = gmin, G2 = gmax, B1 = bmin, B2 = bmax, Count = hist.Length };
    }

    public (VBox, VBox) Split(Span<int> hist)
    {
        int rRange = R2 - R1;
        int gRange = G2 - G1;
        int bRange = B2 - B1;

        if (rRange >= gRange && rRange >= bRange)
            return SplitAlong(hist, 0);
        else if (gRange >= bRange)
            return SplitAlong(hist, 1);
        else
            return SplitAlong(hist, 2);
    }

    private (VBox, VBox) SplitAlong(Span<int> hist, int axis)
    {
        int mid = axis switch
        {
            0 => (R1 + R2) / 2,
            1 => (G1 + G2) / 2,
            _ => (B1 + B2) / 2
        };

        var a = (VBox)MemberwiseClone();
        var b = (VBox)MemberwiseClone();

        if (axis == 0) { a.R2 = mid; b.R1 = mid + 1; }
        if (axis == 1) { a.G2 = mid; b.G1 = mid + 1; }
        if (axis == 2) { a.B2 = mid; b.B1 = mid + 1; }

        return (a, b);
    }

    public Rgb Average(Span<int> hist)
    {
        long rsum = 0, gsum = 0, bsum = 0, total = 0;

        for (int r = R1; r <= R2; r++)
        for (int g = G1; g <= G2; g++)
        for (int b = B1; b <= B2; b++)
        {
            int idx = (r << 10) | (g << 5) | b;
            int h = hist[idx];
            total += h;

            rsum += h * r;
            gsum += h * g;
            bsum += h * b;
        }

        if (total == 0)
            return new Rgb(0, 0, 0);

        return new Rgb(
            (byte)((rsum / total) << 3),
            (byte)((gsum / total) << 3),
            (byte)((bsum / total) << 3));
    }
}
