using Aether.Modding;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>
/// The cursor at the UI's size: vanilla draws it (in UI coordinates) at <c>Main.cursorScale</c>, which
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
}
