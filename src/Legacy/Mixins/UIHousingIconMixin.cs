using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy.Mixins;

/// <summary>The housing page's NPC slots: nine-sliced backgrounds, heads at whole pixels.</summary>
[Mixin(typeof(UIHousingIcon))]
public static class UIHousingIconMixin
{
    [Redirect("DrawIcon(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE, Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)")]
    private static void DrawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth) =>
        PixelSlots.DrawSprite(spriteBatch, texture, position, source, color, rotation, origin, scale, effects, depth);
}
