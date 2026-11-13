using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// No "Inventory", "Coins" or "Ammo" labels; the achievement card sits in the bottom-right corner.
/// </summary>
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

    // Keep the card clear of the crafting panel and independent of the inventory's position.
    [Redirect("DrawInventory()", At.INVOKE, Target = "Terraria.UI.AchievementAdvisor::DrawOneAchievement(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Boolean)", Ordinal = 0)]
    private static void DrawAdvisor(AchievementAdvisor advisor, SpriteBatch spriteBatch, Vector2 position, bool large)
    {
        // The card's 64-texel icon is drawn 8 texels in from the position, and its 72-texel border
        // 4 out from the icon.
        float scale = Sharp.WholeScale(0.75f);
        if (FreshUI.Screens.HudScreen.IsReady)
        {
            advisor.DrawOneAchievement(spriteBatch,
                new Vector2(Main.screenWidth - 10 - 76 * scale, Main.screenHeight - 20 - 76 * scale), large);
            return;
        }
        float columnsCentre = SideButtons.ColumnX + (SideButtons.Pitch + 32f) / 2f;
        float ui = Main.UIScale;
        float left = MathF.Round((columnsCentre - 36f * scale) * ui) / ui;
        advisor.DrawOneAchievement(spriteBatch, new Vector2(left - 4f * scale, 20f - 4f * scale), large);
    }
}
