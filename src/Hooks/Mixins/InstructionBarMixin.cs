using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.UI.Chat;

namespace FreshUI.Hooks.Mixins;

/// <summary>
/// The gamepad's instruction bar (its button hints) has a drop shadow under its outlined text and glyphs, as
/// the hotbar's numbers: the same text a UI pixel lower in translucent black, drawn first.
/// </summary>
[Mixin(typeof(Main))]
public static class InstructionBarMixin
{
    [Redirect("DrawGamepadInstructions()", At.INVOKE,
        Target = "Terraria.UI.Chat.ChatManager::DrawColorCodedStringWithShadow(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2,System.Single,System.Single)")]
    private static void DrawWithDropShadow(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation, Vector2 origin, Vector2 scale, float maxWidth, float spread)
    {
        // As FreshItemSlot's numbers: a UI pixel (two UI units) down, at alpha 100.
        ChatManager.DrawColorCodedStringShadow(batch, font, text, position + new Vector2(0, 2), new Color(0, 0, 0, 100), rotation, origin, scale, maxWidth, spread);
        ChatManager.DrawColorCodedStringWithShadow(batch, font, text, position, color, rotation, origin, scale, maxWidth, spread);
    }
}
