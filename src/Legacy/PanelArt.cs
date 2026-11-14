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

    /// <summary>A menu's title tab (the text panel saying <paramref name="key"/>'s text) at the settings window's title size.</summary>
    public static void TitleSize(UIElement menu, string key)
    {
        foreach (UIElement child in menu.Children)
        {
            if (child is Terraria.GameContent.UI.Elements.UITextPanel<Terraria.Localization.LocalizedText> title && title.Text == Terraria.Localization.Language.GetTextValue(key))
            {
                title.TextScale = TitleScale;
                title.SetText(Terraria.Localization.Language.GetText(key));
                return;
            }
            TitleSize(child, key);
        }
    }

    /// <summary>The large font's scale for a window's title (the settings window's, Gum's FontSize 16).</summary>
    public const float TitleScale = 0.8f;

    /// <summary>A position on whole UI pixels (even UI units, rounded down).</summary>
    public static Vector2 Snap(Vector2 position) => new(Floor2(position.X), Floor2(position.Y));

    /// <summary>A list row's panel in the settings window's rows' colours: <see cref="FreshStyle.ButtonLit"/> with a gold border while lit (hovered), else <see cref="FreshStyle.Button"/> with a black one.</summary>
    public static void LightRow(Terraria.GameContent.UI.Elements.UIPanel row, bool lit)
    {
        row.BackgroundColor = lit ? FreshStyle.ButtonLit : FreshStyle.Button;
        row.BorderColor = lit ? FreshStyle.Gold : Color.Black;
    }

    /// <summary>A sprite's draw position such that its top left (<paramref name="position"/> less its scaled <paramref name="origin"/>) is on a whole UI pixel.</summary>
    public static Vector2 SnapCorner(Vector2 position, Vector2 origin) => Snap(position - origin) + origin;

    /// <summary>A width from <paramref name="x"/> that ends on a whole UI pixel too, measured from the snapped <paramref name="x"/>.</summary>
    public static float SnapWidth(float x, float width) => Round2(x + width) - Floor2(x);

    /// <summary>
    /// An element's area on whole UI pixels: its edges at even UI units, moved inwards (never outside the
    /// area, which a list clips to: a row at its edge keeps its border).
    /// </summary>
    public static CalculatedStyle Snap(CalculatedStyle box)
    {
        int left = Ceiling2(box.X), top = Ceiling2(box.Y);
        return new CalculatedStyle(left, top, Math.Max(0, Floor2(box.X + box.Width) - left), Math.Max(0, Floor2(box.Y + box.Height) - top));
    }

    private static int Ceiling2(float value) => (int)MathF.Ceiling(value / 2) * 2;

    /// <summary>A flat strip on whole UI pixels (an achievement's title and description boxes): its fill, and a black border a UI pixel wide.</summary>
    public static void DrawStrip(SpriteBatch batch, Vector2 position, float width, float height, bool lit)
    {
        Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        Vector2 at = Snap(position);
        int w = (int)SnapWidth(position.X, width), h = (int)SnapWidth(position.Y, height);
        var area = new Rectangle((int)at.X, (int)at.Y, w, h);
        batch.Draw(pixel, area, Color.Black);
        area.Inflate(-2, -2);
        batch.Draw(pixel, area, lit ? StripLit : Strip);
    }

    private static readonly Color Strip = new(21, 28, 61);
    private static readonly Color StripLit = new(39, 51, 104);

    /// <summary>
    /// The scale (UI units per texel) nearest <paramref name="scale"/> at which art drawn by the game at the
    /// UI's scale has a whole number of screen pixels per texel (at least one), or its half for small art.
    /// </summary>
    public static float WholeTexels(float scale)
    {
        float perTexel = scale * Main.UIScale;
        float whole = MathF.Max(0.5f, MathF.Floor(perTexel * 2) / 2);
        if (whole > 1)
            whole = MathF.Floor(whole);
        return whole / Main.UIScale;
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
