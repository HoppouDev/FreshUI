using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy.Mixins;

/// <summary>An equipment slot's corner toggle (accessory, pet and light visibility) is drawn over the slot Gum draws (<see cref="HudScreen.DrawOver"/>).</summary>
[Mixin(typeof(UIEquipToggle))]
public static class UIEquipToggleMixin
{
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color)")]
    private static void Draw(SpriteBatch batch, Texture2D texture, Vector2 position, Color color)
    {
        if (!HudScreen.DrawOver(texture, position, color))
            batch.Draw(texture, position, color);
    }
}
