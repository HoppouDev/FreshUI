using Aether.Modding;
using Aether.UI.Game;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>
/// Hotbar slots drawn at one scale (a little larger than the inventory's), whether selected or not, and
/// with no name at the cursor while hovered (the name above the hotbar shows the held item's).
/// </summary>
[Mixin(typeof(UIHotbarSlot))]
public static class UIHotbarSlotMixin
{
    // The second Main.inventoryScale access in DrawSelf: setting it to the slot's (animated) scale.
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.FIELD, Target = "Terraria.Main::inventoryScale", Ordinal = 1)]
    private static void SetSlotScale(float scale) => Main.inventoryScale = Hud.HotbarScale;

    // DrawSelf writes the name, reads it to append the stack count, then writes it again.
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.FIELD, Target = "Terraria.Main::hoverItemName", Ordinal = 0)]
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.FIELD, Target = "Terraria.Main::hoverItemName", Ordinal = 2)]
    private static void SkipHoverName(string name)
    {
    }
}
