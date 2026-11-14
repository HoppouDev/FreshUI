using FreshUI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace FreshUI.Legacy;

/// <summary>
/// The game's panels in FreshUI's (Gum's) style: on whole UI pixels (two UI units each), as Gum lays
/// everything out, so their 2×2-texel art stays even (<c>UIPanelMixin</c>); and the character-creation
/// panels its option buttons, name fields and description boxes are drawn with (<c>Utils.DrawSplicedPanel</c>,
/// <c>UtilsMixin</c>) as <see cref="FreshPanel"/>'s and <see cref="FreshPanelButton"/>'s art: the panel in
/// the button's tint with a black border, a gold border while selected or hovered.
/// </summary>
public static class PanelArt
{
    private static Texture2D? _grayscale, _selected, _hovered;
    private static Texture2D? _background, _border;
    // The last character-creation panel drawn: a rim drawn inside it (the selected one's is inset) goes at its edge.
    private static Rectangle _lastPanel;

    private static int Floor2(float value) => (int)MathF.Floor(value / 2) * 2;
    private static int Round2(float value) => (int)MathF.Round(value / 2) * 2;

    /// <summary>An element's area on whole UI pixels: its edges at the nearest even UI units (its top left down).</summary>
    public static CalculatedStyle Snap(CalculatedStyle box)
    {
        int left = Floor2(box.X), top = Floor2(box.Y);
        return new CalculatedStyle(left, top, Math.Max(0, Round2(box.X + box.Width) - left), Math.Max(0, Round2(box.Y + box.Height) - top));
    }

    /// <summary>A spliced panel the game draws: in FreshUI's style if it's a character-creation panel (true), else left to the game.</summary>
    public static bool DrawSpliced(SpriteBatch batch, Texture2D texture, int x, int y, int width, int height, Color color)
    {
        _grayscale ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale").Value;
        _selected ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight").Value;
        _hovered ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelBorder").Value;
        _background ??= Main.Assets.Request<Texture2D>(FreshStyle.PanelBackground).Value;
        _border ??= Main.Assets.Request<Texture2D>(FreshStyle.PanelBorder).Value;
        bool panel = texture == _grayscale;
        if (!panel && texture != _selected && texture != _hovered)
            return false;
        CalculatedStyle box = Snap(new CalculatedStyle(x, y, width, height));
        var area = new Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height);
        if (panel)
        {
            Draw(batch, _background, area, color);
            Draw(batch, _border, area, Color.Black * (color.A / 255f));
            _lastPanel = area;
        }
        else if (_lastPanel.Contains(area))
        {
            // A selected or hovered rim over the panel just drawn: the panel's border, gold, at its edge.
            Draw(batch, _border, _lastPanel, FreshStyle.Gold * (color.A / 255f));
        }
        else
        {
            // The art on its own (a description's box): a plain panel.
            Draw(batch, _background, area, FreshStyle.Button * (color.A / 255f));
            Draw(batch, _border, area, Color.Black * (color.A / 255f));
        }
        return true;
    }

    /// <summary>A panel texture (28 texels, corners 12, as UIPanel's) nine-sliced over <paramref name="area"/>, a texel a UI unit.</summary>
    private static void Draw(SpriteBatch batch, Texture2D texture, Rectangle area, Color color)
    {
        const int corner = 12, bar = 4;
        int right = area.Right - corner, bottom = area.Bottom - corner;
        int width = right - area.X - corner, height = bottom - area.Y - corner;
        batch.Draw(texture, new Rectangle(area.X, area.Y, corner, corner), new Rectangle(0, 0, corner, corner), color);
        batch.Draw(texture, new Rectangle(right, area.Y, corner, corner), new Rectangle(corner + bar, 0, corner, corner), color);
        batch.Draw(texture, new Rectangle(area.X, bottom, corner, corner), new Rectangle(0, corner + bar, corner, corner), color);
        batch.Draw(texture, new Rectangle(right, bottom, corner, corner), new Rectangle(corner + bar, corner + bar, corner, corner), color);
        batch.Draw(texture, new Rectangle(area.X + corner, area.Y, width, corner), new Rectangle(corner, 0, bar, corner), color);
        batch.Draw(texture, new Rectangle(area.X + corner, bottom, width, corner), new Rectangle(corner, corner + bar, bar, corner), color);
        batch.Draw(texture, new Rectangle(area.X, area.Y + corner, corner, height), new Rectangle(0, corner, corner, bar), color);
        batch.Draw(texture, new Rectangle(right, area.Y + corner, corner, height), new Rectangle(corner + bar, corner, corner, bar), color);
        batch.Draw(texture, new Rectangle(area.X + corner, area.Y + corner, width, height), new Rectangle(corner, corner, bar, bar), color);
    }
}
