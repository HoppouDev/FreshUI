using Aether.Modding;
using FreshUI.GumUi;
using FreshUI.Screens;

namespace FreshUI;

/// <summary>
/// FreshUI's changes to the game's UI are mixins; the initializer sets up its Gum screens
/// (<see cref="GumHost" />, <see cref="GumScreens" />): the live inventory and hotbar.
/// </summary>
public sealed class Startup : IModInitializer
{
    public const string ModId = "fresh-ui";

    public void Initialize()
    {
        GumHost.Install();
        if (GumGallery.Enabled)
            GumScreens.Register("fresh-ui:gallery", () => new GalleryScreen());
        else
            GumScreens.Register("fresh-ui:hud", () => new HudScreen());
    }
}
