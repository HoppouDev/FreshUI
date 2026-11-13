using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Legacy;

/// <summary>
/// Inventory slots drawn at fractional scales (0.85, 0.6) without uneven pixels: backgrounds are
/// nine-sliced, and item icons drawn at a whole number of screen pixels per art pixel.
/// </summary>
public static class PixelSlots
{
    // How far the slot backgrounds' rounded corners and border reach in from each edge, in texels.
    private const int Corner = 8;

    // Space an icon keeps from its slot's edges (the 2-texel border and one more), in UI units.
    private const float Margin = 3f;

    /// <summary>The inventory's slot pitch: 23 pixels of the art (2 texels each), a 22-pixel slot and a 1-pixel gap.</summary>
    public const float Pitch = 46f;

    /// <summary>A slot <paramref name="scale" /> of the 52-texel slot texture wide, in whole pixels of the art (UI units).</summary>
    public static float Size(float scale) => MathF.Round(52f * scale / 2f) * 2f;

    /// <summary>The width of the slot whose item is being drawn (0: not in a slot).</summary>
    public static float Slot;

    /// <summary>Whether <paramref name="texture" /> drawn like this is an <c>Inventory_Back</c> slot background.</summary>
    public static bool IsSlotBackground(Texture2D texture, Rectangle? source, float rotation, Vector2 origin) =>
        !source.HasValue && rotation == 0f && origin == Vector2.Zero && texture.Width == 52 && texture.Height == 52;

    /// <summary>
    /// Draws a sprite a slot draws: its background (<see cref="IsSlotBackground" />) nine-sliced, anything
    /// else (an item, a silhouette, a head) at whole pixels.
    /// </summary>
    public static void DrawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (IsSlotBackground(texture, source, rotation, origin))
        {
            DrawSlot(spriteBatch, texture, position, color, scale);
            return;
        }
        if (origin == Vector2.Zero && rotation == 0f)
        {
            // Anchored at its top left corner (the trash can), so rounding its scale would move its
            // centre: round it here, keeping the centre where it was.
            float whole = Sharp.WholeScale(scale);
            Vector2 size = (source?.Size() ?? texture.Size());
            position += size * (scale - whole) / 2f;
            scale = whole;
        }
        Sharp.WholePixels(spriteBatch, true);
        spriteBatch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
        Sharp.WholePixels(spriteBatch, false);
    }

    /// <summary>
    /// Draws a slot background at <paramref name="scale" /> with its corners and border at their own
    /// size and only the flat parts between them stretched or shrunk.
    /// </summary>
    public static void DrawSlot(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float scale)
    {
        float ui = Main.UIScale;
        // A whole number of the art's pixels across, so the slot's edges stay on its pixel grid.
        float width = MathF.Round(texture.Width * scale / 2f) * 2f;
        float height = MathF.Round(texture.Height * scale / 2f) * 2f;
        float left = MathF.Round(position.X * ui);
        float top = MathF.Round(position.Y * ui);
        float right = MathF.Round((position.X + width) * ui);
        float bottom = MathF.Round((position.Y + height) * ui);
        float corner = MathF.Round(Corner * ui);
        if (scale == 1f || right - left <= corner * 2f || bottom - top <= corner * 2f)
        {
            spriteBatch.Draw(texture, position, null, color, 0f, default, scale, SpriteEffects.None, 0f);
            return;
        }
        Span<float> xs = stackalloc float[4] { left, left + corner, right - corner, right };
        Span<float> ys = stackalloc float[4] { top, top + corner, bottom - corner, bottom };
        Span<int> us = stackalloc int[4] { 0, Corner, texture.Width - Corner, texture.Width };
        Span<int> vs = stackalloc int[4] { 0, Corner, texture.Height - Corner, texture.Height };
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                var source = new Rectangle(us[column], vs[row], us[column + 1] - us[column], vs[row + 1] - vs[row]);
                var piece = new Vector2((xs[column + 1] - xs[column]) / source.Width, (ys[row + 1] - ys[row]) / source.Height) / ui;
                spriteBatch.Draw(texture, new Vector2(xs[column], ys[row]) / ui, source, color, 0f, default, piece, SpriteEffects.None, 0f);
            }
        }
    }

    /// <summary>
    /// The scale to draw an item icon <paramref name="size" /> texels across (its larger side) at in
    /// the current <see cref="Slot" />, for <paramref name="scale" />. Terraria's sprites draw each art
    /// pixel as 2×2 texels, so it's a multiple of ½ screen pixel per texel: <paramref name="scale" />
    /// rounded up to one, or the largest smaller one that fits inside the slot's border.
    /// </summary>
    public static float IconScale(float scale, float size)
    {
        if (Slot <= 0f || size <= 0f || scale <= 0f)
        {
            return scale;
        }
        float ui = Main.UIScale;
        float limit = (Slot - Margin * 2f) * ui + 0.01f;
        for (float pixels = MathF.Ceiling(scale * ui * 2f - 0.01f) / 2f; pixels >= 0.5f; pixels -= 0.5f)
        {
            if (pixels * size <= limit)
            {
                return pixels / ui;
            }
        }
        return scale;
    }
}
