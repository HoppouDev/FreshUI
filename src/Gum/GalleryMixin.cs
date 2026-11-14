using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.GumUi;

/// <summary>Gum on the title screen for the widget gallery (<see cref="GumGallery"/>): updated after the game, drawn under the cursor.</summary>
[Mixin(typeof(Main))]
public static class GalleryMixin
{
    [Inject("Update(Microsoft.Xna.Framework.GameTime)", At.RETURN)]
    private static void Update(GameTime gameTime, CallbackInfo callback)
    {
        if (GumGallery.IsOpen)
            GumGallery.Update(gameTime);
    }

    [Inject("DrawMenu(Microsoft.Xna.Framework.GameTime)", At.INVOKE, Target = "Terraria.Main::DrawCursor(Microsoft.Xna.Framework.Vector2,System.Boolean)")]
    private static void Draw(GameTime gameTime, CallbackInfo callback)
    {
        if (GumGallery.IsOpen)
            GumHost.Draw(Main.spriteBatch);
    }
}
