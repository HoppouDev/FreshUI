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
            HudScreen.RegisterRoutes();
            GumScreens.Register("fresh-ui:pause", () => new PauseScreen(), layer: GumLayer.Overlay,
                showWhen: () => PauseMenu.ShowsPause, defaultFocus: () => PauseScreen.Current?.First, hints: PauseScreen.Hints);
            Settings("fresh-ui:settings", inGame: true, () => PauseMenu.ShowsSettings);
            Settings("fresh-ui:menu-settings", inGame: false, () => MenuSettingsScreen.Shows);
            GumScreens.Register("fresh-ui:command-popup", () => new CommandPopupScreen(), layer: GumLayer.Overlay, showWhen: () => CommandPopupScreen.Shows);
            GumScreens.Register("fresh-ui:title-menu", () => new TitleMenuScreen(), layer: GumLayer.Overlay,
                showWhen: () => TitleMenuScreen.Shows, defaultFocus: () => TitleMenuScreen.Instance?.First);
            GumScreens.Register("fresh-ui:player-select", () => new PlayerSelectScreen(), layer: GumLayer.Overlay,
                showWhen: PlayerSelectScreen.Shows, defaultFocus: () => PlayerSelectScreen.Instance?.First);
            GumScreens.Register("fresh-ui:world-select", () => new WorldSelectScreen(), layer: GumLayer.Overlay,
                showWhen: WorldSelectScreen.Shows, defaultFocus: () => WorldSelectScreen.Instance?.First);
            GumScreens.Register("fresh-ui:world-creation", () => new WorldCreationScreen(), layer: GumLayer.Overlay,
                showWhen: WorldCreationScreen.Shows, defaultFocus: () => WorldCreationScreen.Instance?.First);
            GumScreens.Register("fresh-ui:character-creation", () => new CharacterCreationScreen(), layer: GumLayer.Overlay,
                showWhen: CharacterCreationScreen.Shows, defaultFocus: () => CharacterCreationScreen.Instance?.First);
            GumScreens.Register("fresh-ui:mods", () => new ModsScreen(), layer: GumLayer.Overlay,
                showWhen: () => ModsScreen.Shows, defaultFocus: () => ModsScreen.Instance?.First);
            GumScreens.Register("fresh-ui:world-load", () => new WorldLoadScreen(), layer: GumLayer.Overlay, showWhen: WorldLoadScreen.Shows);
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
