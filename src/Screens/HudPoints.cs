using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The HUD's Gum controls on the game's input: where an element is in the game's UI units (the game's cursor,
/// the mouse or the gamepad's, is in those), and the gamepad's points placed on them (the game's link points,
/// which the D-pad moves between), A on the focused one acting.
/// </summary>
internal static class HudPoints
{
    // The element's area in UI units (two a UI pixel).
    public static Rectangle Units(GraphicalUiElement element) => new((int)(element.AbsoluteLeft * 2), (int)(element.AbsoluteTop * 2),
        (int)(element.GetAbsoluteWidth() * 2), (int)(element.GetAbsoluteHeight() * 2));

    public static bool Hovered(GraphicalUiElement element) =>
        Units(element).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;

    // The gamepad's focus on the element's point (the game's): lit, and A on it is true.
    public static bool Place(GraphicalUiElement element, int point, out bool focused)
    {
        float centre = float.NaN;
        return Place(element, point, out focused, ref centre);
    }

    // A wide element (wideX not NaN) has its point under or over the gamepad's, within it, so the D-pad's
    // moves by position find it; kept where it was while it's the gamepad's.
    public static bool Place(GraphicalUiElement element, int point, out bool focused, ref float wideX)
    {
        Rectangle area = Units(element);
        Vector2 at = area.Center.ToVector2();
        if (!float.IsNaN(wideX))
        {
            int current = UILinkPointNavigator.CurrentPoint;
            if (current != point && UILinkPointNavigator.Points.TryGetValue(current, out UILinkPoint? from))
                wideX = Math.Clamp(from.Position.X / Main.UIScale, area.Left + area.Height / 2f, area.Right - area.Height / 2f);
            at.X = Math.Clamp(wideX, area.Left, area.Right);
        }
        UILinkPointNavigator.SetPosition(point, at);
        focused = PlayerInput.UsingGamepadUI && UILinkPointNavigator.InUse && UILinkPointNavigator.CurrentPoint == point;
        if (!focused || !Main.mouseLeft || !Main.mouseLeftRelease)
            return false;
        Main.mouseLeftRelease = false;
        return true;
    }
}
