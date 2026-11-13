using Aether.Api;
using Aether.Modding;
using FreshUI.GumUi;
using FreshUI.Screens;

namespace FreshUI;

/// <summary>
/// FreshUI's changes to the game's UI are mixins; the initializer sets up its Gum screens
/// (<see cref="GumHost" />, <see cref="GumScreens" />): the sample inventory, shown with F9.
/// </summary>
public sealed class Startup : IModInitializer
{
    public const string ModId = "fresh-ui";

    public void Initialize()
    {
        GumHost.Install();
        GumScreen inventory = GumScreens.Register("fresh-ui:inventory", () => new InventoryScreen(), initiallyVisible: false);
        Keybinds.Register("fresh-ui:inventory", "F9", () => inventory.IsVisible = !inventory.IsVisible);
    }
}
