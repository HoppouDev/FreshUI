using Aether.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace FreshUI.Legacy;

/// <summary>
/// The cursor at integer scaling (<c>CursorMixin</c>): each of its sprites (the cursor, its shadow, the
/// thick cursor's outline, the gamepad's cursors) at a whole number of screen pixels per texel (the UI
/// scale rounded, at least one), point-sampled, from the cursor's point on a whole screen pixel; each
/// sprite's offset from the point (the outline's and shadow's single texels) scaled with it.
/// </summary>
public static class PixelCursor
{
    private static bool _sampled;

    /// <summary>Screen pixels per texel of the cursor's art.</summary>
    public static int PixelsPerTexel => Math.Max(1, (int)MathF.Round(Main.UIScale, MidpointRounding.AwayFromZero));

    /// <summary>Draws one of the cursor's sprites, as the game would at <paramref name="scale"/> (in UI units), at integer scaling.</summary>
    public static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (!_sampled)
        {
            _sampled = true;
            foreach (var cursor in TextureAssets.Cursors)
            {
                if (cursor?.IsLoaded == true)
                    cursor.Value.SamplerEXT = SamplerState.PointClamp;
            }
        }
        float ui = Main.UIScale;
        float perTexel = PixelsPerTexel / ui;
        // The point on a whole screen pixel; the sprite's offset from it in texels, a texel now perTexel units.
        Vector2 anchor = CursorHooks.GetDrawPosition();
        Vector2 snapped = new Vector2(MathF.Round(anchor.X * ui), MathF.Round(anchor.Y * ui)) / ui;
        Vector2 at = snapped + (position - anchor) * perTexel;
        // A smaller sprite (the smart cursor's dimmed second pointer, 0.3) keeps its share of the size.
        float size = scale < 1f ? scale * perTexel : perTexel;
        batch.Draw(texture, at, source, color, rotation, origin, size, effects, depth);
    }
}
