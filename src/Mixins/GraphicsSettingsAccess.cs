using Aether.Graphics.Terrain;
using Aether.Modding;

namespace FreshUI.Mixins;

/// <summary>Aether's own graphics settings (an <c>internal</c> type), for the settings window's Aether Graphics tab.</summary>
[Mixin("Aether.Graphics.GraphicsSettings")]
public interface IGraphicsSettingsAccess
{
    [Accessor("UncappedFps", IsStatic = true)] bool GetUncappedFps();
    [Accessor("UncappedFps", IsStatic = true)] void SetUncappedFps(bool value);
    [Accessor("BackgroundLighting", IsStatic = true)] bool GetBackgroundLighting();
    [Accessor("BackgroundLighting", IsStatic = true)] void SetBackgroundLighting(bool value);
    [Accessor("TerrainMode", IsStatic = true)] RendererMode GetTerrainMode();
    [Accessor("TerrainMode", IsStatic = true)] void SetTerrainMode(RendererMode value);
    [Accessor("TerrainQuality", IsStatic = true)] RendererQuality GetTerrainQuality();
    [Accessor("TerrainQuality", IsStatic = true)] void SetTerrainQuality(RendererQuality value);
    [Accessor("ComputeTerrain", IsStatic = true)] bool GetComputeTerrain();
    [Accessor("ComputeTerrain", IsStatic = true)] void SetComputeTerrain(bool value);
    [Accessor("FastTerrain", IsStatic = true)] bool GetFastTerrain();
    [Accessor("FastTerrain", IsStatic = true)] void SetFastTerrain(bool value);
    [Accessor("SmoothEntityMovement", IsStatic = true)] bool GetSmoothEntityMovement();
    [Accessor("SmoothEntityMovement", IsStatic = true)] void SetSmoothEntityMovement(bool value);
}
