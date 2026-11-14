using System.Reflection.Metadata;
using FreshUI.GumUi;
using Terraria;

[assembly: MetadataUpdateHandler(typeof(FreshUI.HotReload))]

namespace FreshUI;

/// <summary>
/// After a .NET hot reload (<c>./aether watch</c>) changes FreshUI's code, its screens are built again
/// on the game thread, so an edit to how they're built shows at once: every Gum screen. Mixin method bodies are merged into the game when
/// it starts, so edits to them need a restart.
/// </summary>
internal static class HotReload
{
    // Called by .NET's hot reload agent, off the game thread, after it applies an update.
    internal static void UpdateApplication(Type[]? types) => Main.QueueMainThreadAction(Rebuild);

    private static void Rebuild()
    {
        foreach (GumScreen screen in GumScreens.All)
        {
            screen.Rebuild();
        }
        Console.WriteLine("FreshUI: hot reload applied, screens rebuilt");
    }
}
