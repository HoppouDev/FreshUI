using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The NPC chat and sign box is a <see cref="FreshPanel"/> (drawn by Gum where the game drew its
/// <c>ChatBack</c> art, its top part and bottom edge), under the game's text, portrait and buttons.
/// </summary>
[Mixin(typeof(UINpcChat))]
public static class UINpcChatMixin
{
    private const string Draw = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color)";

    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE, Target = Draw)]
    private static void DrawBox(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color) =>
        NpcChatBox.Draw(batch, texture, position, source);
}
