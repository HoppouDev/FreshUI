using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace FreshUI;

/// <summary>
/// UI images drawn at the UI's scale. Most of Terraria's UI art has 2×2 texels per pixel, which the
/// GUI scale keeps even; some (the character and world list buttons) has a texel per pixel, which
/// a half-step UI scale (1.5) can't: those are drawn at a whole number of screen pixels per texel,
/// rounded down so they keep within their place, and centred on it. The list items' buttons, laid out
/// again for it (<see cref="Respace" />), are drawn twice that size from GUI scale 3.
/// </summary>
public static class PixelImages
{
    private static readonly ConditionalWeakTable<Texture2D, object> Doubled = new();

    private static readonly ConditionalWeakTable<UIElement, object> Respaced = new();

    private static readonly object Yes = new(), No = new();

    /// <summary>The image button being drawn (its DrawSelf sets it).</summary>
    public static UIElement? Drawing;

    // Screen pixels per texel of single-texel art: the whole number up to the UI scale.
    private static float Whole => MathF.Max(1f, MathF.Floor(Main.UIScale + 0.001f));

    // For the list items' buttons: twice that from GUI scale 3.
    private static float Large => GuiScale.Current >= 3 ? 2f * Whole : Whole;

    public static void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color)
    {
        float ui = Main.UIScale;
        bool respaced = Drawing != null && Respaced.TryGetValue(Drawing, out _);
        float whole = respaced ? Large : Whole;
        if (whole == ui || IsDoubled(texture))
        {
            spriteBatch.Draw(texture, position, source, color);
            return;
        }
        float scale = whole / ui;
        Vector2 size = source?.Size() ?? texture.Size();
        // A respaced button's box is the image's size; others are centred on theirs.
        Vector2 offset = respaced ? Vector2.Zero : size * (1f - scale) / 2f;
        spriteBatch.Draw(texture, position + offset, source, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Lays a character or world list item's buttons (single-texel art, 24 units apart along its
    /// bottom, delete at the right) out again at the size they're drawn at, 2 units apart, with the
    /// labels beside them.
    /// </summary>
    public static void Respace(UIElement item)
    {
        float ui = Main.UIScale;
        float x = float.NaN, units = 0f;
        foreach (UIElement child in item.Children.Where(child => child is UIImageButton && child.HAlign == 0f).OrderBy(child => child.Left.Pixels).ToList())
        {
            units = child.Width.Pixels * Large / ui;
            if (float.IsNaN(x))
                x = child.Left.Pixels;
            Resize(child, units);
            child.Left.Set(x, 0f);
            x += units + 2f;
        }
        if (float.IsNaN(x))
            return;
        foreach (UIElement child in item.Children)
        {
            if (child is UIImageButton && child.HAlign == 1f)
                Resize(child, units);
            else if (child is UIText && child.VAlign == 1f)
                child.Left.Set(child.HAlign == 1f ? -(units + 8f) : x + 2f, 0f);
        }
    }

    private static void Resize(UIElement button, float units)
    {
        button.Width.Set(units, 0f);
        button.Height.Set(units, 0f);
        Respaced.AddOrUpdate(button, units);
    }

    /// <summary>Gives a respaced button its size back after <c>SetImage</c> sizes it to the image.</summary>
    public static void KeepSize(UIElement button)
    {
        if (Respaced.TryGetValue(button, out object? units))
            Resize(button, (float)units);
    }

    // Whether every 2×2 block of the texture is one colour (pixel art at 2 texels a pixel).
    private static bool IsDoubled(Texture2D texture)
    {
        if (Doubled.TryGetValue(texture, out object? known))
            return known == Yes;
        bool doubled = true;
        if (texture.Format == SurfaceFormat.Color && texture.Width % 2 == 0 && texture.Height % 2 == 0)
        {
            var texels = new Color[texture.Width * texture.Height];
            texture.GetData(texels);
            int width = texture.Width;
            for (int y = 0; y < texture.Height && doubled; y += 2)
            {
                for (int x = 0; x < width && doubled; x += 2)
                {
                    Color c = texels[y * width + x];
                    doubled = texels[y * width + x + 1] == c && texels[(y + 1) * width + x] == c && texels[(y + 1) * width + x + 1] == c;
                }
            }
        }
        else if (texture.Format == SurfaceFormat.Color)
        {
            doubled = false;
        }
        Doubled.AddOrUpdate(texture, doubled ? Yes : No);
        return doubled;
    }
}
