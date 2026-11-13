using Aether.Modding;
using Aether.UI.Game;
using Terraria;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// Hotbar slots drawn at one scale (a little larger than the inventory's), whether selected or not, and
/// with no name at the cursor while hovered (the name above the hotbar shows the held item's).
/// </summary>
[Mixin(typeof(UIHotbarSlot))]
public class UIHotbarSlotMixin
{
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Terraria.UI.ItemSlot::Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch,Terraria.Item[],System.Int32,System.Int32,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color)")]
    private void DrawSlot(SpriteBatch batch, Item[] items, int context, int index, Vector2 position, Color color)
    {
        if (!HudScreen.Present(this, items, context, index, position))
            ItemSlot.Draw(batch, items, context, index, position, color);
    }

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
