using System.Reflection.Metadata;
using FreshUI.GumUi;
using Terraria;

[assembly: MetadataUpdateHandler(typeof(FreshUI.HotReload))]

namespace FreshUI;

/// <summary>
/// After a .NET hot reload (<c>./aether watch</c>) changes FreshUI's code, its screens are built again on the game
/// thread, so an edit to a component or screen shows at once, in the running game: the textures FreshUI makes are made
/// again first (<see cref="GumTextures.Made"/>, list icons). .NET doesn't run static initializers again, so values meant
/// to be tweaked live (colours) are getters, not <c>static readonly</c> fields. Mixin method bodies are merged into the
/// game when it starts, and edits .NET can't apply in place (a rude edit) restart it.
/// </summary>
internal static class HotReload
{
    // Called by .NET's hot reload agent, off the game thread, after it applies an update.
    internal static void UpdateApplication(Type[]? types) => Main.QueueMainThreadAction(Rebuild);

    private static void Rebuild()
    {
        // What FreshUI made from the old code (art it generates) is made again from the new.
        GumTextures.ClearMade();
        FreshUI.Hooks.PixelImages.ClearIcons();
        foreach (GumScreen screen in GumScreens.All)
        {
            screen.Rebuild();
        }
        Console.WriteLine("FreshUI: hot reload applied, screens rebuilt");
    }
}
