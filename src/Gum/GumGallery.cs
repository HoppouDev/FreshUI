using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.GumUi;

/// <summary>
/// FreshUI's widget gallery: launched with <see cref="Parameter"/> (<c>./aether gallery</c>), the client
/// opens on a gallery of FreshUI's Gum components in place of the title menu, to prototype them with
/// .NET hot reload and drive them over Aether's MCP server. Gum runs on the title screen then
/// (<see cref="GumHostMixin"/>), drawn under the cursor, and the gallery takes the gamepad
/// (<see cref="GumGamepad"/>), starting at its first control.
/// </summary>
public static class GumGallery
{
    public const string Parameter = "-freshuigallery";

    /// <summary>Whether the client was launched for the gallery (from the command line, read before Terraria's own parsing).</summary>
    public static bool Enabled { get; } = Environment.GetCommandLineArgs().Contains(Parameter);

    /// <summary>On the title menu in a gallery launch, once the game has loaded (its splash screen is over).</summary>
    public static bool IsOpen => Enabled && Main.gameMenu && !Main.showSplash && Main.menuMode is 0 or 888;

    public static void Update(GameTime time)
    {
        // The title menu shows nothing under the gallery (an empty UI state), so its buttons take no clicks.
        if (Main.menuMode == 0)
            Main.menuMode = 888;
        if (Main.menuMode == 888 && Main.MenuUI.CurrentState != null)
            Main.MenuUI.SetState(null);
        GumHost.UpdateIfIdle(time);
    }
}
