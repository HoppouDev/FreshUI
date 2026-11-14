using FreshUI.Components;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy;

/// <summary>The NPC chat and sign box, a <see cref="FreshPanel"/> in place of the game's <c>ChatBack</c> art (see <c>UINpcChatMixin</c>).</summary>
public static class NpcChatBox
{
    private static readonly GumInline<FreshPanel> Box = new(() => new FreshPanel());

    /// <summary>
    /// One of the box's two draws: its top part (from the art's top, as tall as the text) gives the
    /// whole box, with the 30-unit bottom edge drawn after it; that edge is the panel's own.
    /// </summary>
    public static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source)
    {
        if (source is { Y: 0 } top)
            Box.Draw(batch, new Rectangle((int)position.X, (int)position.Y, texture.Width, top.Height + 30));
    }
}
