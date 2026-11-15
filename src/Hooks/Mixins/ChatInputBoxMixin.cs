using Aether.Modding;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FreshUI.Hooks.Mixins;

/// <summary>The chat input's box is FreshUI's text field panel (<see cref="ChatInputBox"/>).</summary>
[Mixin(typeof(Main))]
public static class ChatInputBoxMixin
{
    [Redirect("DrawPlayerChat()", At.INVOKE, Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)")]
    private static void BoxPiece(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth) =>
        ChatInputBox.Piece(batch, texture, position, source, color, rotation, origin, scale, effects, depth);
}
