using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The trash slot under the inventory's last column, on its pixel grid, at the end of the coin and
/// ammo row; with a chest open, a row lower, with the chest (<see cref="MainMixin.ChestDrop" />).
/// </summary>
[Mixin(typeof(UITrashSlot))]
public static class UITrashSlotMixin
{
    [Redirect("UpdateState()", At.INVOKE, Target = "Aether.UI.Game.UIGameItemSlot::Place(Microsoft.Xna.Framework.Vector2,System.Single)")]
    private static void PlaceSlot(UIGameItemSlot slot, Vector2 topLeft, float scale)
    {
        // Vanilla's (448, 258): the tenth column's, a sixth row down, at 47.6 a slot.
        if (topLeft == new Vector2(448f, 258f))
            topLeft = new Vector2(20f + 9f * PixelSlots.Pitch, UICoinAmmoSlotsMixin.Top);
        else if (topLeft == new Vector2(453f, 426f))
            topLeft.Y += MainMixin.ChestDrop;
        slot.Place(topLeft, scale);
    }
}
