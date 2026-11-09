using Microsoft.Xna.Framework.Graphics;

namespace FreshUI;

/// <summary>
/// FreshUI's sharp UI: the UI's sprite batches sample the nearest texel (Terraria's UI is pixel art
/// it draws scaled, which its linear filtering blurs), and FNA's SpriteBatch puts sprites' edges on
/// whole pixels (Aether's <c>SamplerState.PixelSnapEXT</c>). Fonts keep linear filtering (their
/// textures' <c>SamplerEXT</c>).
/// </summary>
public static class Sharp
{
    public static readonly SamplerState Snapped = new SamplerState
    {
        Filter = TextureFilter.Point,
        AddressU = TextureAddressMode.Clamp,
        AddressV = TextureAddressMode.Clamp,
        AddressW = TextureAddressMode.Clamp,
        PixelSnapEXT = true
    };

    /// <summary>
    /// Has <paramref name="spriteBatch" /> draw evenly scaled pixel art at a whole number of screen
    /// pixels per pixel of the art (½ pixel per texel; Aether's <c>SpriteBatch.PixelScaleEXT</c>) while
    /// <paramref name="on" /> is, for art drawn at fractional scales.
    /// </summary>
    public static void WholePixels(SpriteBatch spriteBatch, bool on) => spriteBatch.PixelScaleEXT = on ? 0.5f : 0f;

    /// <summary>
    /// <paramref name="scale" /> rounded to a whole number of screen pixels per pixel of the art (½ a
    /// pixel per texel), for art drawn as several sprites that must stay in step.
    /// </summary>
    public static float WholeScale(float scale)
    {
        if (scale <= 0f)
            return scale;
        float ui = Terraria.Main.UIScale;
        return MathF.Max(0.5f, MathF.Round(scale * ui * 2f) / 2f) / ui;
    }
}
