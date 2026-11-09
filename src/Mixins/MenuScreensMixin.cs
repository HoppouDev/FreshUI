using Aether.Modding;
using Aether.UI.Menu;

namespace FreshUI.Mixins;

/// <summary>The title menu's Settings and every settings page are FreshUI's tabbed settings window (<see cref="MenuSettingsScreen" />).</summary>
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
    }
}
