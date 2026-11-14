using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>The game's UI box (<c>DrawInvBG</c>) is FreshUI's panel (<see cref="InventoryBox"/>).</summary>
[Mixin(typeof(Utils))]
public static class UtilsMixin
{
    [Inject("DrawInvBG(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Int32,System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Color)", At.HEAD, Cancellable = true)]
    private static void DrawBox(SpriteBatch batch, int x, int y, int width, int height, Color color, CallbackInfo callback)
    {
        if (InventoryBox.Draw(batch, x, y, width, height, color))
            callback.Cancel();
    }
}
