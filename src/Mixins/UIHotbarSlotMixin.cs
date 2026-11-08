using Aether.Modding;
using Aether.UI.Game;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>Hotbar slots drawn at the inventory's scale, whether selected or not.</summary>
[Mixin(typeof(UIHotbarSlot))]
public static class UIHotbarSlotMixin
{
    // The second Main.inventoryScale access in DrawSelf: setting it to the slot's (animated) scale.
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.FIELD, Target = "Terraria.Main::inventoryScale", Ordinal = 1)]
    private static void SetSlotScale(float scale) => Main.inventoryScale = Hud.SlotScale;
}
