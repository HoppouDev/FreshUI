using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace FreshUI.Legacy;

/// <summary>
/// UI images drawn at the UI's scale. Most of Terraria's UI art has 2×2 texels per pixel, which the
/// GUI scale keeps even; some (the character and world list buttons) has a texel per pixel, which
/// a half-step UI scale (1.5) can't: those are drawn at a whole number of screen pixels per texel,
/// rounded down so they keep within their place, and centred on it. The list items' buttons, laid out
/// again for it (<see cref="Respace" />), are drawn at the GUI scale's screen pixels per texel, as
/// just their icons (the buttons' frame and fill dropped).
/// </summary>
public static class PixelImages
{
    private static readonly ConditionalWeakTable<Texture2D, object> Doubled = new();

    private static readonly ConditionalWeakTable<UIElement, object> Respaced = new();

    private static readonly ConditionalWeakTable<Texture2D, Texture2D> Icons = new();

    private static readonly object Yes = new(), No = new();

    /// <summary>The image button being drawn (its DrawSelf sets it).</summary>
    public static UIElement? Drawing;

    // Screen pixels per texel of single-texel art: the whole number up to the UI scale.
    private static float Whole => MathF.Max(1f, MathF.Floor(Main.UIScale + 0.001f));

    // For the list items' buttons: the GUI scale (a pixel of their art the size of the rest's).
    private static float Large => GuiScale.Current;

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
        if (respaced && source == null)
            texture = Icon(texture);
        float scale = whole / ui;
        Vector2 size = source?.Size() ?? texture.Size();
        // A respaced button's box is the image's size; others are centred on theirs.
        Vector2 offset = respaced ? Vector2.Zero : size * (1f - scale) / 2f;
        spriteBatch.Draw(texture, position + offset, source, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    // A label's UIText, at scale 1: 16 units tall, its line drawn 2 units above its top.
    private const float LabelLineHeight = 16f;

    private const float LabelTextLift = 2f;

    /// <summary>
    /// Lays a character or world list item's buttons (single-texel art, 24 units apart along its
    /// bottom, delete at the right) out again at the size they're drawn at, 2 units apart, with the
    /// labels beside them, the item taller by what they gained, and its left column as far from the
    /// left as the delete button is from the right.
    /// </summary>
    public static void Respace(UIElement item)
    {
        float ui = Main.UIScale;
        float x = float.NaN, units = 0f, image = 0f;
        // Vanilla insets the left column (portrait, buttons) 4 units more than the delete button on the right.
        foreach (UIElement child in item.Children.Where(child => child.HAlign == 0f))
            child.Left.Set(child.Left.Pixels - 4f, child.Left.Precent);
        foreach (UIElement child in item.Children.Where(child => child is UIImageButton && child.HAlign == 0f).OrderBy(child => child.Left.Pixels).ToList())
        {
            image = child.Height.Pixels;
            units = child.Width.Pixels * Large / ui;
            if (float.IsNaN(x))
                x = child.Left.Pixels;
            Resize(child, units);
            child.Left.Set(x, 0f);
            x += units + 2f;
        }
        if (float.IsNaN(x))
            return;
        // The buttons sit at the bottom: the item grows by what they gained, keeping the space above them.
        if (units > image)
            item.Height.Set(item.Height.Pixels + units - image, item.Height.Precent);
        foreach (UIElement child in item.Children)
        {
            if (child is UIImageButton && child.HAlign == 1f)
                Resize(child, units);
            else if (child is UIText && child.VAlign == 1f)
            {
                child.Left.Set(child.HAlign == 1f ? -(units + 8f) : x + 2f, 0f);
                // Its capitals level with the buttons' middle (they sit on the item's bottom).
                child.Top.Set(LabelLineHeight + LabelTextLift - SdfText.MouseTextCapMiddle - units / 2f, 0f);
            }
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

    // The list items' button art without its button: the 2-pixel frame (outline and bevel), the fill
    // (its colour inside the frame's top left) and the icon's shadow on it (the right bevel's colour).
    private static Texture2D Icon(Texture2D texture)
    {
        if (Icons.TryGetValue(texture, out Texture2D? icon))
            return icon;
        icon = texture;
        int width = texture.Width, height = texture.Height;
        if (texture.Format == SurfaceFormat.Color && width > 6 && height > 6)
        {
            var texels = new Color[width * height];
            texture.GetData(texels);
            Color fill = texels[3 * width + 3], shadow = texels[3 * width + width - 2];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int edge = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, height - 1 - y));
                    bool corner = Math.Min(x, width - 1 - x) == 2 && Math.Min(y, height - 1 - y) == 2;
                    Color c = texels[y * width + x];
                    if (edge < 2 || corner || c == fill || c == shadow)
                        texels[y * width + x] = Color.Transparent;
                }
            }
            icon = new Texture2D(texture.GraphicsDevice, width, height);
            icon.SetData(texels);
        }
        Icons.AddOrUpdate(texture, icon);
        return icon;
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
