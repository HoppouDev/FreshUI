using Aether.Graphics.Terrain;
using Aether.Modding;
using FreshUI.Legacy.Mixins;
using Terraria;

namespace FreshUI.Legacy;

/// <summary>
/// Aether's graphics settings (the title menu's Aether Graphics page), through <see cref="IGraphicsSettingsAccess" />:
/// the settings themselves are <c>internal</c> to the game.
/// </summary>
public static class AetherGraphics
{
    private static readonly Type Target = typeof(Main).Assembly.GetType("Aether.Graphics.GraphicsSettings", throwOnError: true)!;

    private static T Bind<T>(string name) where T : Delegate => MixinAccess.BindStatic<T>(Target, typeof(IGraphicsSettingsAccess), name);

    public static readonly Func<RendererMode> GetTerrainMode = Bind<Func<RendererMode>>("GetTerrainMode");

    public static readonly Action<RendererMode> SetTerrainMode = Bind<Action<RendererMode>>("SetTerrainMode");

    public static readonly Func<RendererQuality> GetTerrainQuality = Bind<Func<RendererQuality>>("GetTerrainQuality");

    public static readonly Action<RendererQuality> SetTerrainQuality = Bind<Action<RendererQuality>>("SetTerrainQuality");

    public static readonly Flag ComputeTerrain = new("ComputeTerrain", "Compute");

    public static readonly Flag FastTerrain = new("FastTerrain", "FastTerrain");

    public static readonly Flag BackgroundLighting = new("BackgroundLighting", "Lighting");

    public static readonly Flag SmoothEntityMovement = new("SmoothEntityMovement", "Motion");

    public static readonly Flag UncappedFps = new("UncappedFps", "Uncapped");

    /// <summary>One of the on/off settings, with its label's key under <c>AetherGraphics</c>.</summary>
    public sealed class Flag(string field, string key)
    {
        public readonly Func<bool> Get = Bind<Func<bool>>("Get" + field);

        public readonly Action<bool> Set = Bind<Action<bool>>("Set" + field);

        public readonly string Key = "AetherGraphics." + key;
    }

    /// <summary>Turns uncapped FPS on or off, and vsync with it once the frame is done (as Aether's page does).</summary>
    public static void ToggleUncapped()
    {
        UncappedFps.Set(!UncappedFps.Get());
        Main.QueueMainThreadAction(() =>
        {
            Main.graphics.SynchronizeWithVerticalRetrace = !UncappedFps.Get();
            Main.graphics.ApplyChanges();
        });
    }
}
