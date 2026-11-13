using Aether.Modding;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The sampler every UI sprite batch uses (Aether's <c>PixelArt.Sampler</c>, which answers with the
/// batch's own, linear by default): FreshUI's sharp, pixel-snapping one (<see cref="Sharp" />).
/// </summary>
[Mixin("Aether.Graphics.PixelArt")]
public static class PixelArtMixin
{
    [Inject("Sampler(Microsoft.Xna.Framework.Graphics.SamplerState)", At.RETURN, Cancellable = true)]
    private static void Snap(SamplerState requested, CallbackInfoReturnable<SamplerState> callback)
    {
        callback.SetReturnValue(Sharp.Snapped);
    }
}
