using System.Diagnostics;
using Aether.UI.Game;
using Terraria;

namespace FreshUI.Legacy;

/// <summary>The bottom-centre hotbar's layout.</summary>
public static class Hud
{
    /// <summary>The scale the inventory's slots are drawn at (44 UI units).</summary>
    public const float SlotScale = 0.85f;

    /// <summary>The hotbar's slots' size: 4 UI units (2 pixels of the art) larger than the inventory's.</summary>
    public const float HotbarSlotSize = 48f;

    /// <summary>The scale every hotbar slot is drawn at, selected or not: <see cref="HotbarSlotSize" /> of the 52-texel slot.</summary>
    public const float HotbarScale = HotbarSlotSize / 52f;

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
    public const float Height = BottomMargin + HotbarSlotSize + NameSpace;

    /// <summary>How far above the slots the held item's name is drawn (UI units).</summary>
    public const float NameLift = 4f;

    /// <summary>
    /// An item's name colour, as vanilla's hover text and tooltips have it (Main's
    /// <c>MouseText_DrawItemTooltip_GetItemNameColor</c>), without the mouse text's brightness pulse.
    /// </summary>
    public static Microsoft.Xna.Framework.Color NameColor(Item item)
    {
        int rare = item.expert ? -12 : item.rare;
        Microsoft.Xna.Framework.Color color = rare switch
        {
            -13 => new(255, (byte)(Main.masterColor * 200f), 0),
            -12 => new((byte)Main.DiscoR, (byte)Main.DiscoG, (byte)Main.DiscoB),
            -11 => new(255, 175, 0),
            -1 => new(130, 130, 130),
            1 => new(150, 150, 255),
            2 => new(150, 255, 150),
            3 => new(255, 200, 150),
            4 => new(255, 150, 150),
            5 => new(255, 150, 255),
            6 => new(210, 160, 255),
            7 => new(150, 255, 10),
            8 => new(255, 255, 10),
            9 => new(5, 200, 255),
            10 => new(255, 40, 100),
            >= 11 => new(180, 40, 255),
            _ => Microsoft.Xna.Framework.Color.White
        };
        return color;
    }

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
