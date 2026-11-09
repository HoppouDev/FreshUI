using Microsoft.Xna.Framework.Graphics;

namespace FreshUI;

/// <summary>
/// Aether's sharp UI (point sampling for UI sprite batches), followed: whether it's on, and the
/// sampler FreshUI uses instead, which also has FNA's SpriteBatch put sprites' edges on whole pixels
/// (Aether's <c>SamplerState.PixelSnapEXT</c>).
/// </summary>
public static class Sharp
{
    /// <summary>Whether Aether's sharp UI was on when it was last asked for a UI sampler.</summary>
    public static bool Enabled;

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
    /// <paramref name="on" /> and sharp UI are, for art drawn at fractional scales.
    /// </summary>
    public static void WholePixels(SpriteBatch spriteBatch, bool on) => spriteBatch.PixelScaleEXT = on && Enabled ? 0.5f : 0f;

    /// <summary>The sampler for a UI sprite batch that would otherwise use <paramref name="requested" />.</summary>
    public static SamplerState? Sampler(SamplerState? requested) => Enabled ? Snapped : requested;
}
