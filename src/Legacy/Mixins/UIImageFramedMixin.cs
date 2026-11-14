using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Legacy.Mixins;

/// <summary>The game's framed UI images (achievement icons among them) on whole UI pixels.</summary>
[Mixin(typeof(UIImageFramed))]
public static class UIImageFramedMixin
{
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color)")]
    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color) =>
        batch.Draw(texture, PanelArt.Snap(position), source, color);
}
