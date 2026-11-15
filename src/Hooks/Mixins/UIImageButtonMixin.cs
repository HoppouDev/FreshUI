using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Hooks.Mixins;

/// <summary>Image buttons (and their hover borders) with even pixels (<see cref="PixelImages" />).</summary>
[Mixin(typeof(UIImageButton))]
public static class UIImageButtonMixin
{
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color)")]
    private static void DrawImage(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color) =>
        PixelImages.Draw(spriteBatch, texture, position, source, color);
}
