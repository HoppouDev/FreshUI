using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>No "Inventory" label above the open inventory.</summary>
[Mixin(typeof(Main))]
public static class InventoryLabelMixin
{
    // The first text DrawInventory draws: the label.
    [Redirect("DrawInventory()", At.INVOKE, Target = "ReLogic.Graphics.DynamicSpriteFontExtensionMethods::DrawString(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single,Microsoft.Xna.Framework.Vector2[],Microsoft.Xna.Framework.Color[])", Ordinal = 0)]
    private static void SkipLabel(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, Vector2[] offsets, Color[] colors)
    {
    }
}
