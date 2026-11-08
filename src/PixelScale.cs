namespace FreshUI;

/// <summary>Whole-number UI scales, so every texel covers the same number of pixels.</summary>
public static class PixelScale
{
    /// <summary><paramref name="scale" /> rounded to a whole number, at most what fits the screen (<paramref name="max" />).</summary>
    public static float Whole(float scale, float max)
    {
        float whole = MathF.Max(1f, MathF.Round(scale, MidpointRounding.AwayFromZero));
        return MathF.Min(whole, MathF.Max(1f, MathF.Floor(max)));
    }
}
