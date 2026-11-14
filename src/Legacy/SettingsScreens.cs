using Aether.UI.Menu;
using FreshUI.Screens;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace FreshUI.Legacy;

/// <summary>
/// The settings window on the title menu (a Gum screen, <see cref="SettingsScreen"/>, over this empty one): the screen for Settings (<c>menuMode</c> 11) and for each of
/// the settings pages it replaces (opening the page's tab), so code that still opens a page by its
/// mode (Keybindings' Back opens Controls) lands on the window.
/// </summary>
public sealed class MenuSettingsScreen : UIMenuScreen
{
    private static MenuSettingsScreen? _instance;

    public static MenuSettingsScreen Instance => _instance ??= new MenuSettingsScreen();

    // The menuMode last seen: a change to a page's mode opens its tab.
    private int _mode = -1;

    /// <summary>Whether the title menu shows the settings (<see cref="FreshUI.Screens.SettingsScreen"/>, drawn by Gum over this empty screen).</summary>
    public static bool Shows => Main.gameMenu && (Main.menuMode == 11 || SettingsTabs.ForMenuMode(Main.menuMode).HasValue);

    protected override void BuildItems()
    {
    }

    public override void UpdateState()
    {
        if (Main.menuMode != _mode)
        {
            _mode = Main.menuMode;
            if (SettingsTabs.ForMenuMode(_mode) is SettingsTab tab)
            {
                SettingsTabs.Tab = tab;
            }
        }
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
