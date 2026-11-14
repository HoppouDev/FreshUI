using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>The game's UI box (<c>DrawInvBG</c>) is FreshUI's panel (<see cref="InventoryBox"/>), and its character-creation panels are in FreshUI's style (<see cref="PanelArt"/>).</summary>
[Mixin(typeof(Utils))]
public static class UtilsMixin
{
    [Inject("DrawInvBG(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Int32,System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Color)", At.HEAD, Cancellable = true)]
    private static void DrawBox(SpriteBatch batch, int x, int y, int width, int height, Color color, CallbackInfo callback)
    {
        if (InventoryBox.Draw(batch, x, y, width, height, color))
            callback.Cancel();
    }

    [Inject("DrawSplicedPanel(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Graphics.Texture2D,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Color)", At.HEAD, Cancellable = true)]
    private static void DrawSpliced(SpriteBatch batch, Texture2D texture, int x, int y, int width, int height, int left, int right, int top, int bottom, Color color, CallbackInfo callback)
    {
        if (PanelArt.DrawSpliced(batch, texture, x, y, width, height, color))
            callback.Cancel();
    }
}
