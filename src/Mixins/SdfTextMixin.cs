using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>Text from signed distance fields (<see cref="SdfText" />), for each font once it has loaded.</summary>
[Mixin(typeof(Main))]
public static class SdfTextMixin
{
    [Inject("DoDraw(Microsoft.Xna.Framework.GameTime)", At.HEAD)]
    private static void ReplaceGlyphs(GameTime gameTime, CallbackInfo callback) => SdfText.Update();
}
