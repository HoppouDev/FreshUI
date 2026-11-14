using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.GumUi;

/// <summary>
/// Gum where the HUD's retained layer doesn't reach: its update after the game's whenever that layer
/// didn't run it (the title menu, the pause menu and settings…), and the overlay layer (menus) drawn
/// once a frame: in game before mouse text (over the HUD and the game's own windows, under tooltips and
/// the cursor), and otherwise just before the cursor (the title menu, and the settings window and fancy
/// UIs, which stop the interface's layers before mouse text). The widget gallery runs here too
/// (<see cref="GumGallery"/>).
/// </summary>
[Mixin(typeof(Main))]
public static class GumHostMixin
{
    [Inject("Update(Microsoft.Xna.Framework.GameTime)", At.RETURN)]
    private static void Update(GameTime gameTime, CallbackInfo callback)
    {
        if (GumGallery.IsOpen)
            GumGallery.Update(gameTime);
        else
            GumHost.UpdateIfIdle(gameTime);
    }

    [Inject("Draw(Microsoft.Xna.Framework.GameTime)", At.HEAD)]
    private static void BeginFrame(GameTime gameTime, CallbackInfo callback) => GumHost.BeginFrame();

    [Inject("DrawCursor(Microsoft.Xna.Framework.Vector2,System.Boolean)", At.HEAD)]
    private static void DrawUnderCursor(Vector2 bonus, bool smart, CallbackInfo callback) => GumHost.DrawOverlay(Main.spriteBatch);

    [Inject("DrawInterface_33_MouseText()", At.HEAD)]
    private static void DrawInGame(CallbackInfo callback) => GumHost.DrawOverlay(Main.spriteBatch);
}
