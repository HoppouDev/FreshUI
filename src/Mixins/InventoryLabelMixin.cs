using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>No "Inventory", "Coins" or "Ammo" labels on the open inventory.</summary>
[Mixin(typeof(Main))]
public static class InventoryLabelMixin
{
    private const string DrawString = "ReLogic.Graphics.DynamicSpriteFontExtensionMethods::DrawString(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single,Microsoft.Xna.Framework.Vector2[],Microsoft.Xna.Framework.Color[])";

    // The first text DrawInventory draws: the label.
    [Redirect("DrawInventory()", At.INVOKE, Target = DrawString, Ordinal = 0)]
    private static void SkipLabel(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, Vector2[] offsets, Color[] colors)
    {
    }

    // The second and third: "Coins" and "Ammo", over the columns vanilla has and FreshUI doesn't (its
    // coin and ammo row under the inventory is told apart by its slots' colours).
    [Redirect("DrawInventory()", At.INVOKE, Target = DrawString, Ordinal = 1)]
    private static void SkipCoinsLabel(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, Vector2[] offsets, Color[] colors)
    {
    }

    [Redirect("DrawInventory()", At.INVOKE, Target = DrawString, Ordinal = 2)]
    private static void SkipAmmoLabel(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, Vector2[] offsets, Color[] colors)
    {
    }
}
