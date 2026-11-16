using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Hooks;

/// <summary>
/// UI images drawn at the UI's scale. Most of Terraria's UI art has 2×2 texels per pixel, which the
/// GUI scale keeps even; some (the list buttons) has a texel per pixel, which a half-step UI scale
/// (1.5) can't: those are drawn at a whole number of screen pixels per texel, rounded down so they
/// keep within their place, and centred on it. <see cref="Icon"/> is a list button's art as just its
/// icon (the button's frame and fill dropped), as the select screens draw them.
/// </summary>
public static class PixelImages
{
    private static readonly ConditionalWeakTable<Texture2D, object> Doubled = new();

    private static readonly ConditionalWeakTable<Texture2D, Texture2D> Icons = new();

    private static readonly object Yes = new(), No = new();

    // Screen pixels per texel of single-texel art: the whole number up to the UI scale.
    private static float Whole => MathF.Max(1f, MathF.Floor(Main.UIScale + 0.001f));

    public static void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color)
    {
        float ui = Main.UIScale, whole = Whole;
        if (whole == ui || IsDoubled(texture))
        {
            spriteBatch.Draw(texture, position, source, color);
            return;
        }
        float scale = whole / ui;
        Vector2 size = source?.Size() ?? texture.Size();
        Vector2 offset = size * (1f - scale) / 2f;
        spriteBatch.Draw(texture, position + offset, source, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>Forgets the icons made from list buttons' art (after a hot reload, so an edit to <see cref="Icon"/> shows).</summary>
    internal static void ClearIcons() => Icons.Clear();

    // The list items' button art without its button: the 2-pixel frame (outline and bevel), the fill
    // (its colour inside the frame's top left) and the icon's shadow on it (the right bevel's colour).
    public static Texture2D Icon(Texture2D texture)
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
