using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Mixins;

/// <summary>The in-game settings window is FreshUI's pause menu (<see cref="PauseScreen" />) or its tabbed settings (<see cref="GameSettingsScreen" />), the same as the title menu's.</summary>
[Mixin(typeof(UIIngameOptions))]
public static class UIIngameOptionsMixin
{
    // Drawn where Aether's window was, with what it draws over it (the cursor, mouse text) as before.
    [Redirect("DrawWindow(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE, Target = "Aether.UI.Game.GameScreens::Draw(Aether.UI.Game.UIGameScreen,Microsoft.Xna.Framework.Graphics.SpriteBatch)")]
    private static void DrawSettings(UIGameScreen screen, SpriteBatch spriteBatch) => GameScreens.Draw(PauseMenu.Screen, spriteBatch);

    // IngameOptions.Open: the next draw opens it afresh.
    [Inject("Opened()", At.HEAD)]
    private static void OpenSettings(CallbackInfo callback) => PauseMenu.Opened();
}
