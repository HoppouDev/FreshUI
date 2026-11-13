using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;

namespace FreshUI.Legacy.Mixins;

[Mixin(typeof(UIRecipeList))]
public static class UIRecipeListMixin
{
    [Redirect("DrawIcons(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "ReLogic.Graphics.DynamicSpriteFontExtensionMethods::DrawString(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single,Microsoft.Xna.Framework.Vector2[],Microsoft.Xna.Framework.Color[])")]
    private static void DrawFilterTitle(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position,
        Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth,
        Vector2[] offsets, Color[] colors)
    {
        if (text != Lang.inter[25].Value)
            batch.DrawString(font, text, position, color, rotation, origin, scale, effects, depth, offsets, colors);
    }
}
