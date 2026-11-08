using Aether.Modding;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Mixins;

/// <summary>Aether's UI sampler, with pixel snapping while Aether's sharp UI is on.</summary>
[Mixin("Aether.Graphics.PixelArt")]
public static class PixelArtMixin
{
    [Inject("Sampler(Microsoft.Xna.Framework.Graphics.SamplerState)", At.RETURN, Cancellable = true)]
    private static void Snap(SamplerState requested, CallbackInfoReturnable<SamplerState> callback)
    {
        // Sharp UI answers with point sampling for every UI batch.
        Sharp.Enabled = callback.ReturnValue == SamplerState.PointClamp;
        if (Sharp.Enabled)
            callback.SetReturnValue(Sharp.Snapped);
    }
}
