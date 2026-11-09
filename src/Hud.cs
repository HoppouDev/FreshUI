using System.Diagnostics;
using Aether.UI.Game;
using Terraria;

namespace FreshUI;

/// <summary>The bottom-centre hotbar's layout.</summary>
public static class Hud
{
    /// <summary>The scale every hotbar slot is drawn at: the inventory's.</summary>
    public const float SlotScale = 0.85f;

    /// <summary>Between hotbar slots: one pixel of the art (2 texels).</summary>
    public const float SlotGap = 2f;

    /// <summary>How far the hotbar's bottom edge is from the bottom of the screen.</summary>
    public const float BottomMargin = 4f;

    /// <summary>Room above the slots for the held item's name, drawn at <see cref="NameScale" />.</summary>
    public const float NameSpace = 20f;

    public const float NameScale = 1.0f;

    /// <summary>How long the held item's name shows after it changes, then how long it fades, in ticks.</summary>
    public const float NameShown = 120f;

    public const float NameFade = 30f;

    /// <summary>How long (ticks) the hotbar can go undrawn before it counts as appearing again.</summary>
    public const float HotbarGap = 10f;

    /// <summary>The space the hotbar and the name above it take up at the bottom of the screen.</summary>
    public const float Height = BottomMargin + 44f + NameSpace;

    /// <summary>The hotbar's bottom edge, as last laid out (UI units).</summary>
    public static float Bottom;

    /// <summary>How much higher the boss health bar goes to stay above the hotbar.</summary>
    public static float BossBarClearance => UIInventorySlots.Enabled && !Main.playerInventory ? Height : 0f;

    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    /// <summary>Game ticks (1/60 s) since the last call, for animations drawn every frame.</summary>
    public static float TicksSince(ref long last)
    {
        long now = Clock.ElapsedTicks;
        float ticks = last == 0 ? 0f : (float)((now - last) * 60.0 / Stopwatch.Frequency);
        last = now;
        return ticks;
    }
}
