using Aether.Modding;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;

namespace FreshUI.Hooks.Mixins;

/// <summary>Hover text and tooltips' lines are drawn in Gum's text (<see cref="TooltipText"/>) at each tooltip's end.</summary>
[Mixin(typeof(Main))]
public class MouseTextMixin
{
    private const string FreshTooltipInnerSelector = "MouseTextInner(Terraria.Main/MouseTextCache)";
    private const string FreshTooltipItemSelector = "MouseText_DrawItemTooltip(Terraria.Main/MouseTextCache,System.Int32,System.Byte,System.Int32,System.Int32)";
    private const string FreshTooltipBuffSelector = "MouseText_DrawBuffTooltip(System.String,System.Int32&,System.Int32&,Microsoft.Xna.Framework.Vector2)";
    private const string FreshTooltipShadowedSelector = "Terraria.UI.Chat.ChatManager::DrawColorCodedStringWithShadow(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2,System.Single,System.Single)";
    private const string FreshTooltipDrawStringSelector = "ReLogic.Graphics.DynamicSpriteFontExtensionMethods::DrawString(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,System.String,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single,Microsoft.Xna.Framework.Vector2[],Microsoft.Xna.Framework.Color[])";

    [Redirect(FreshTooltipInnerSelector, At.INVOKE, Target = FreshTooltipShadowedSelector)]
    [Redirect(FreshTooltipItemSelector, At.INVOKE, Target = FreshTooltipShadowedSelector)]
    private static void Line(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, float maxWidth, float spread) =>
        TooltipText.Line(batch, font, text, position, color, rotation, origin, scale, maxWidth, spread);

    [Redirect(FreshTooltipBuffSelector, At.INVOKE, Target = FreshTooltipDrawStringSelector)]
    private static void BuffLine(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale,
        SpriteEffects effects, float depth, Vector2[] offsets, Color[] colors) =>
        TooltipText.OutlinedLine(batch, font, text, position, color, rotation, origin, scale, effects, depth, offsets, colors);

    [Inject(FreshTooltipInnerSelector, At.RETURN)]
    private void InnerEnd(CallbackInfo callback) => TooltipText.Flush(Main.spriteBatch);
}
