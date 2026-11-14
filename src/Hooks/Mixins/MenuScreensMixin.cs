using Aether.Modding;
using Aether.UI.Menu;
using FreshUI.GumUi;
using FreshUI.Legacy;
using FreshUI.Screens;

namespace FreshUI.Hooks.Mixins;

/// <summary>The title menu's Settings and every settings page are FreshUI's tabbed settings window (<see cref="MenuSettingsScreen" />), and the modes Gum screens replace an empty screen (<see cref="GumRoutes.MenuMode"/>).</summary>
[Mixin(typeof(MenuScreens))]
public static class MenuScreensMixin
{
    [Inject("GetScreen(System.Int32)", At.HEAD, Cancellable = true)]
    private static void SettingsScreen(int mode, CallbackInfoReturnable<UIMenuScreen> callback)
    {
        if (mode == 11 || SettingsTabs.ForMenuMode(mode).HasValue)
        {
            callback.SetReturnValue(MenuSettingsScreen.Instance);
        }
        else if (GumRoutes.IsMenuMode(mode))
        {
            callback.SetReturnValue(EmptyMenuScreen.Instance);
        }
    }
}
