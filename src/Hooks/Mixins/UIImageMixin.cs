using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Hooks.Mixins;

/// <summary>The game's UI images with their art's top left on whole UI pixels (<see cref="PanelArt.SnapCorner"/>), so 2×2-texel art stays even.</summary>
[Mixin(typeof(UIImage))]
public static class UIImageMixin
{
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)")]
    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth) =>
        batch.Draw(texture, rotation == 0 ? PanelArt.SnapCorner(position, origin * scale) : position, source, color, rotation, origin, scale, effects, depth);
}
