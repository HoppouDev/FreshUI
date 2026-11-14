using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy.Mixins;

/// <summary>The in-game settings window is FreshUI's pause menu or its settings (Gum screens on the overlay, <see cref="FreshUI.Screens.PauseScreen" /> and <see cref="FreshUI.Screens.SettingsScreen" />), so Aether's window isn't drawn.</summary>
[Mixin(typeof(UIIngameOptions))]
public static class UIIngameOptionsMixin
{
    [Redirect("DrawWindow(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE, Target = "Aether.UI.Game.GameScreens::Draw(Aether.UI.Game.UIGameScreen,Microsoft.Xna.Framework.Graphics.SpriteBatch)")]
    private static void DrawSettings(UIGameScreen screen, SpriteBatch spriteBatch)
    {
    }

    // IngameOptions.Open: the next draw opens it afresh.
    [Inject("Opened()", At.HEAD)]
    private static void OpenSettings(CallbackInfo callback) => PauseMenu.Opened();
}
