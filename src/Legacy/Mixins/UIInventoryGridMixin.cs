using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;
using FreshUI.Screens;
using Terraria;
using Terraria.GameInput;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The inventory's slots on the art's pixel grid: a pitch of 46 UI units (23 art pixels; vanilla's
/// 56 × 0.85 = 47.6, truncated, alternates 47 and 48), so with 44-unit slots the gaps are exactly one
/// art pixel.
/// </summary>
[Mixin(typeof(UIInventoryGrid))]
public static class UIInventoryGridMixin
{
    [Inject("UpdateState()", At.HEAD, Cancellable = true)]
    private static void UpdateGrid(CallbackInfo callback)
    {
        if (!HudScreen.TrySlotPosition(0, 10, out Vector2 first))
            return;
        callback.Cancel();
        Main.inventoryScale = Hud.SlotScale;
        var area = new Rectangle((int)first.X - 46, (int)first.Y, 550, 228);
        if (area.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
            Main.LocalPlayer.mouseInterface = true;
    }

    private const float Origin = 20f;

    private const float VanillaPitch = 56f * 0.85f;

    [Redirect("OnInitialize()", At.INVOKE, Target = "Aether.UI.Game.UIGameItemSlot::Place(Microsoft.Xna.Framework.Vector2,System.Single)")]
    private static void PlaceSlot(UIGameItemSlot slot, Vector2 topLeft, float scale)
    {
        float column = MathF.Round((topLeft.X - Origin) / VanillaPitch);
        float row = MathF.Round((topLeft.Y - Origin) / VanillaPitch);
        slot.Place(new Vector2(Origin + column * PixelSlots.Pitch, Origin + row * PixelSlots.Pitch), scale);
    }
}
