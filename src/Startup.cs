using Aether.Modding;
using FreshUI.GumUi;
using FreshUI.Legacy;
using FreshUI.Screens;

namespace FreshUI;

/// <summary>
/// FreshUI's changes to the game's UI are mixins; the initializer sets up its Gum screens
/// (<see cref="GumHost" />, <see cref="GumScreens" />): the live inventory and hotbar, the pause menu
/// and the settings window.
/// </summary>
public sealed class Startup : IModInitializer
{
    public const string ModId = "fresh-ui";

    public void Initialize()
    {
        GumHost.Install();
        if (GumGallery.Enabled)
            GumScreens.Register("fresh-ui:gallery", () => new GalleryScreen(), layer: GumLayer.Overlay, defaultFocus: GalleryScreen.First);
        else
        {
            GumScreens.Register("fresh-ui:hud", () => new HudScreen());
            GumScreens.Register("fresh-ui:pause", () => new PauseScreen(), layer: GumLayer.Overlay,
                showWhen: () => PauseMenu.ShowsPause, defaultFocus: () => PauseScreen.Current?.First, hints: PauseScreen.Hints);
            Settings("fresh-ui:settings", inGame: true, () => PauseMenu.ShowsSettings);
            Settings("fresh-ui:menu-settings", inGame: false, () => MenuSettingsScreen.Shows);
            GumScreens.Register("fresh-ui:command-popup", () => new CommandPopupScreen(), layer: GumLayer.Overlay, showWhen: () => CommandPopupScreen.Shows);
        }
    }

    // The settings window, in game or on the title menu, taking the gamepad (LT/RT switch tabs).
    private static void Settings(string id, bool inGame, Func<bool> showWhen)
    {
        SettingsScreen? screen = null;
        GumScreens.Register(id, () => screen = new SettingsScreen(inGame), layer: GumLayer.Overlay, showWhen: showWhen,
            defaultFocus: () => screen?.DefaultFocus, hints: () => screen?.Hints() ?? "", pageMove: direction => screen?.SwitchTab(direction));
    }
}
