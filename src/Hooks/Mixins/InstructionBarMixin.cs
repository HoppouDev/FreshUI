using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.UI.Chat;

namespace FreshUI.Hooks.Mixins;

/// <summary>
/// The gamepad's instruction bar (its button hints): split across the screen, the first hints at the bottom left as the
/// game's and the rest at the bottom right, about half the width each, each hint its glyphs and text without the game's
/// colon between them, the hints a little closer than the game's three spaces; with a drop shadow under its outlined
/// text and glyphs, as the hotbar's numbers: the same text a UI pixel lower in translucent black, drawn first.
/// </summary>
[Mixin(typeof(Main))]
public static class InstructionBarMixin
{
    [Redirect("DrawGamepadInstructions()", At.INVOKE,
        Target = "Terraria.UI.Chat.ChatManager::DrawColorCodedStringWithShadow(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2,System.Single,System.Single)")]
    private static void DrawSplit(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation, Vector2 origin, Vector2 scale, float maxWidth, float spread)
    {
        // The game's hints: "glyphs: text" each, three spaces after each (BuildCommand and the link points' own).
        string[] hints = text.Replace("]: ", "] ").Split("   ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        float total = ChatManager.GetStringSize(font, string.Join(Gap, hints), scale).X;
        int split = 0;
        float width = 0;
        // Left until the left side would pass half of all of it (at least one hint each side, with two or more).
        while (split < hints.Length - 1 && (split == 0 || width + ChatManager.GetStringSize(font, hints[split], scale).X <= total / 2))
            width += ChatManager.GetStringSize(font, hints[split++] + Gap, scale).X;
        if (hints.Length < 2)
            split = hints.Length;
        Draw(batch, font, string.Join(Gap, hints[..split]), position, color, rotation, origin, scale, maxWidth, spread);
        if (split < hints.Length)
        {
            string right = string.Join(Gap, hints[split..]);
            float x = Main.screenWidth - position.X - ChatManager.GetStringSize(font, right, scale).X;
            Draw(batch, font, right, new Vector2(x, position.Y), color, rotation, origin, scale, maxWidth, spread);
        }
    }

    // Between hints: two spaces (the game's three, a little closer).
    private const string Gap = "  ";

    private static void Draw(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation, Vector2 origin, Vector2 scale, float maxWidth, float spread)
    {
        // As FreshItemSlot's numbers: a UI pixel (two UI units) down, at alpha 100.
        ChatManager.DrawColorCodedStringShadow(batch, font, text, position + new Vector2(0, 2), new Color(0, 0, 0, 100), rotation, origin, scale, maxWidth, spread);
        ChatManager.DrawColorCodedStringWithShadow(batch, font, text, position, color, rotation, origin, scale, maxWidth, spread);
    }
}
