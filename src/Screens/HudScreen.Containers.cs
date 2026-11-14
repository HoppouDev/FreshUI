using Aether.UI.Game;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The open container's slots over the inventory (Aether's <see cref="UIChestSlots"/>: a chest, piggy
/// bank, safe, defender's forge or void vault, scrolled when it has more than 40; and
/// <see cref="UIShopSlots"/>: an NPC's shop), on the inventory's grid, each handled by <c>ItemSlot.Handle</c>.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> ChestAsked = GumRoutes.GameScreen<UIChestSlots>();
    private static readonly Func<bool> ShopAsked = GumRoutes.GameScreen<UIShopSlots>();
    private readonly object[] _containerOwners = Enumerable.Range(0, 40).Select(_ => new object()).ToArray();

    private void PresentContainers()
    {
        if (!IsReady)
            return;
        Player player = Main.LocalPlayer;
        // (The route still shows for an update after the chest closes.)
        (Chest? chest, int context) = player.chest switch
        {
            -2 => (player.bank, 4),
            -3 => (player.bank2, 4),
            -4 => (player.bank3, 4),
            -5 => (player.bank4, 32),
            >= 0 => (Main.chest[player.chest], 3),
            _ => (null, 0),
        };
        if (chest != null && ChestAsked())
        {
            Rectangle area = ChestUI.LastChestDisplayRectangle = ContainerArea();
            if (area.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
                player.mouseInterface = true;
            int highestRow = (int)Math.Max(0.0, Math.Ceiling(chest.maxItems / 10f) - 4.0);
            ChestUI.StartingRowForDrawing = Utils.Clamp(ChestUI.StartingRowForDrawing, 0, highestRow);
            ChestUI.LastHighestChestRow = highestRow;
            ItemSlot.PrepareForChest(chest);
            ContainerSlots(chest.item, context, ChestUI.StartingRowForDrawing * 10);
        }
        else if (ShopAsked())
        {
            if (ContainerArea().Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
                player.mouseInterface = true;
            ContainerSlots(Main.instance.shop[Main.npcShop].item, 15, 0);
        }
    }

    // The 10×4 grid's area over the inventory, in UI units.
    private static Rectangle ContainerArea() => new((int)((GroupOrigin.X + Pitch) * 2), (int)(GroupOrigin.Y * 2), (int)((10 * Pitch - 1) * 2), (int)((4 * Pitch - 1) * 2));

    private void ContainerSlots(Item[] items, int context, int first)
    {
        for (int i = 0; i < 40; i++)
        {
            int index = first + i;
            if (index >= items.Length || !TrySlotPosition(context, index, out Vector2 position))
                continue;
            if (new Rectangle((int)position.X, (int)position.Y, (int)(Cell * 2), (int)(Cell * 2)).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.inventoryScale = Components.FreshItemSlot.DefaultScale;
                ItemSlot.Handle(items, context, index);
            }
            Present(_containerOwners[i], items, context, index, position);
        }
    }
}
