using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Mixins;

/// <summary>
/// Aether's small icon buttons (the hotbar lock, builder toggles, equipment page icons) at whole
/// pixels: each draws its icon at its own fractional scale.
/// </summary>
[Mixin(typeof(UIIconButton))]
public static class UIIconButtonMixin
{
    [Inject("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD)]
    private static void Before(SpriteBatch spriteBatch, CallbackInfo callback) => Sharp.WholePixels(spriteBatch, true);

    [Inject("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.RETURN)]
    private static void After(SpriteBatch spriteBatch, CallbackInfo callback) => Sharp.WholePixels(spriteBatch, false);
}
