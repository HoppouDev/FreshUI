using Aether.UI.Game;
using Aether.UI.Menu;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace FreshUI;

/// <summary>
/// The settings window on the title menu: the screen for Settings (<c>menuMode</c> 11) and for each of
/// the settings pages it replaces (opening the page's tab), so code that still opens a page by its
/// mode (Keybindings' Back opens Controls) lands on the window.
/// </summary>
public sealed class MenuSettingsScreen : UIMenuScreen
{
    private static MenuSettingsScreen? _instance;

    public static MenuSettingsScreen Instance => _instance ??= new MenuSettingsScreen();

    private SettingsWindow? _window;

    // The menuMode last seen: a change to a page's mode opens its tab.
    private int _mode = -1;

    protected override void BuildItems()
    {
        _window = new SettingsWindow(inGame: false);
        Append(_window);
    }

    public override void UpdateState()
    {
        if (Main.menuMode != _mode)
        {
            _mode = Main.menuMode;
            if (SettingsTabs.ForMenuMode(_mode) is SettingsTab tab)
            {
                SettingsWindow.Tab = tab;
            }
        }
        _window?.UpdateState();
    }

    public override void GoBack() => Back();

    /// <summary>Back to the main menu, saving the settings (as vanilla's Settings menu's Back does).</summary>
    public static void Back()
    {
        SoundEngine.PlaySound(SoundID.MenuClose);
        Main.menuMode = 0;
        Main.SaveSettings();
    }
}

/// <summary>The settings window in game (the pause menu's Settings, or the inventory's), over the game dimmed.</summary>
public sealed class GameSettingsScreen : UIGameScreen
{
    private static GameSettingsScreen? _instance;

    public static GameSettingsScreen Instance => _instance ??= new GameSettingsScreen();

    private SettingsWindow? _window;

    public override void OnActivate()
    {
        RemoveAllChildren();
        _window = new SettingsWindow(inGame: true);
        Append(_window);
    }

    public override void UpdateState() => _window?.UpdateState();

    protected override void DrawSelf(SpriteBatch spriteBatch) => PauseScreen.Dim(spriteBatch, this);
}
