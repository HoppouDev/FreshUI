using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent.UI;
using Terraria.UI.Chat;

namespace FreshUI.Hooks.Mixins;

/// <summary>
/// The loading tips (under the world generation's progress) have a drop shadow under their outlined text, as the
/// instruction bar's: the same text a UI pixel lower in translucent black, drawn first. They stay the game's
/// chat text, as they can hold glyph tags.
/// </summary>
[Mixin(typeof(GameTipsDisplay))]
public static class GameTipsDisplayMixin
{
    [Redirect("Draw()", At.INVOKE,
        Target = "Terraria.UI.Chat.ChatManager::DrawColorCodedStringWithShadow(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2,System.Single,System.Single)")]
    private static void DrawTipWithDropShadow(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation, Vector2 origin, Vector2 scale, float maxWidth, float spread)
    {
        ChatManager.DrawColorCodedStringShadow(batch, font, text, position + new Vector2(0, 2), new Color(0, 0, 0, 100), rotation, origin, scale, maxWidth, spread);
        ChatManager.DrawColorCodedStringWithShadow(batch, font, text, position, color, rotation, origin, scale, maxWidth, spread);
    }
}
