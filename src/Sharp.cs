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

    /// <summary>The sampler for a UI sprite batch that would otherwise use <paramref name="requested" />.</summary>
    public static SamplerState? Sampler(SamplerState? requested) => Enabled ? Snapped : requested;
}
