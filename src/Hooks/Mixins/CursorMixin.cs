using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Hooks.Mixins;

/// <summary>
/// The cursor at integer scaling (<see cref="PixelCursor"/>), and at the UI's size: vanilla draws it (in UI coordinates) at <c>Main.cursorScale</c>, which
/// pulses between about 0.8 and 1.1, so its pixels are never the UI's. Read as 1 where the cursor is
/// drawn; the pulse still sets its colour, and the classic hearts that also use it still pulse.
/// </summary>
[Mixin(typeof(Main))]
public static class CursorMixin
{
    private const string CursorScale = "Terraria.Main::cursorScale";

    [Redirect("DrawCursor(Microsoft.Xna.Framework.Vector2,System.Boolean)", At.FIELD, Target = CursorScale)]
    private static float DrawCursorScale() => 1f;

    [Redirect("DrawThickCursor(System.Boolean)", At.FIELD, Target = CursorScale)]
    private static float ThickCursorScale() => 1f;

    [Redirect("DrawInterface_36_Cursor()", At.FIELD, Target = CursorScale)]
    private static float OverrideCursorScale() => 1f;

    [Redirect("DrawInterface_38_MouseCarriedObject()", At.FIELD, Target = CursorScale, Ordinal = 0)]
    private static float CarriedItemScale() => 1f;

    private const string SpriteDraw = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)";

    // Every sprite of the cursor at integer scaling (PixelCursor).
    [Redirect("DrawCursor(Microsoft.Xna.Framework.Vector2,System.Boolean)", At.INVOKE, Target = SpriteDraw)]
    private static void Cursor(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth) =>
        PixelCursor.Draw(batch, texture, position, source, color, rotation, origin, scale, effects, depth);

    [Redirect("DrawThickCursor(System.Boolean)", At.INVOKE, Target = SpriteDraw)]
    private static void Outline(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth) =>
        PixelCursor.Draw(batch, texture, position, source, color, rotation, origin, scale, effects, depth);
}
