using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;

namespace FreshUI.Mixins;

/// <summary>
/// The coin and ammo slots as a row under the inventory, at its size and pitch (vanilla: two columns
/// of small slots beside it): the coins under its first four columns, the ammo under the next four, the
/// trash under its last (<see cref="UITrashSlotMixin" />).
/// </summary>
[Mixin(typeof(UICoinAmmoSlots))]
public static class UICoinAmmoSlotsMixin
{
    // Vanilla's: columns at 497 and 534, rows from 105 every 56 × 0.6.
    private const float VanillaCoinX = 497f;

    private const float VanillaTop = 105f;

    private const float VanillaPitch = 56f * 0.6f;

    /// <summary>The row's top: the inventory grid's sixth row.</summary>
    public const float Top = 20f + 5f * PixelSlots.Pitch;

    [Redirect("OnInitialize()", At.INVOKE, Target = "Aether.UI.Game.UIGameItemSlot::Place(Microsoft.Xna.Framework.Vector2,System.Single)")]
    private static void PlaceSlot(UIGameItemSlot slot, Vector2 topLeft, float scale)
    {
        float first = topLeft.X == VanillaCoinX ? 0f : 4f;
        float index = MathF.Round((topLeft.Y - VanillaTop) / VanillaPitch);
        slot.Place(new Vector2(20f + (first + index) * PixelSlots.Pitch, Top), Hud.SlotScale);
    }
}
