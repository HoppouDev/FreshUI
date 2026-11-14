using FreshUI.Components;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Legacy;

/// <summary>
/// The game's box for its UI (<c>Utils.DrawInvBG</c>: tooltips' opaque box, popups, the invasion
/// progress, the dresser, capture mode…) as a <see cref="FreshPanel"/> in its colour, drawn by Gum
/// on the spot (see <c>UtilsMixin</c>). Boxes too small for the panel's corners, and ones drawn
/// outside the UI's space, keep the game's art.
/// </summary>
public static class InventoryBox
{
    // The panel's rounded corners (12 texels, 6 UI pixels, 12 UI units) both ways.
    private const int MinimumSize = 24;

    private static readonly GumInline<FreshPanel> Box = new(() => new FreshPanel());

    /// <summary>Draws the box as a panel, if it can: false leaves it to the game.</summary>
    public static bool Draw(SpriteBatch batch, int x, int y, int width, int height, Color color)
    {
        if (!GumHost.IsInitialized || width < MinimumSize || height < MinimumSize || GumHost.BatchTransform(batch) != Main.UIScaleMatrix)
            return false;
        if (color == default)
            color = new Color(63, 65, 151, 255) * 0.785f;
        FreshPanel panel = Box.Element;
        Color border = Color.Black * (color.A / 255f);
        // Gum's setters notify (and allocate) even for an unchanged value.
        if (panel.BackgroundColor != color)
            panel.BackgroundColor = color;
        if (panel.BorderColor != border)
            panel.BorderColor = border;
        Box.Draw(batch, new Rectangle(x, y, width, height));
        return true;
    }
}
