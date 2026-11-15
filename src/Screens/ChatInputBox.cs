using FreshUI.Components;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace FreshUI.Screens;

/// <summary>
/// The chat input's box (the game's <c>TextBack</c> art, a strip along the bottom of the screen) as a
/// <see cref="FreshTextBox"/>'s panel being typed in: lit, with a gold border, drawn on the spot under the
/// game's text (<c>ChatInputBoxMixin</c>). The line itself stays the game's (its chat tags: item links, colours).
/// </summary>
public static class ChatInputBox
{
    private static readonly GumInline<FreshPanel> Box = new(() => new FreshPanel { BackgroundColor = FreshStyle.ButtonLit, BorderColor = FreshStyle.Gold });

    /// <summary>One of the box's pieces as the game draws it; the first draws the whole box, the rest nothing.</summary>
    public static void Piece(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (texture != TextureAssets.TextBack.Value || !GumHost.IsInitialized)
        {
            batch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
            return;
        }
        if (source is not { X: 0 })
            return;
        int width = Main.screenWidth > 800 ? Main.screenWidth - 300 : texture.Width;
        Box.Draw(batch, new Rectangle((int)position.X, (int)position.Y, width, texture.Height));
    }
}
