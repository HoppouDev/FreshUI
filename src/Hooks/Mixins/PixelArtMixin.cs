using Aether.Modding;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Hooks.Mixins;

/// <summary>
/// The sampler every UI sprite batch uses (Aether's <c>PixelArt.Sampler</c>, which answers with the
/// batch's own, linear by default): FreshUI's sharp, pixel-snapping one (<see cref="Sharp" />).
/// </summary>
[Mixin("Aether.Graphics.PixelArt")]
public static class PixelArtMixin
{
    // An overwrite, not an inject: it's called for every UI element drawn, and an inject's callback is an allocation each.
    [Overwrite]
    internal static SamplerState Sampler(SamplerState requested) => Sharp.Snapped;
}
