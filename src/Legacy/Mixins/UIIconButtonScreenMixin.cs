using Aether.Modding;
using Aether.UI.Game;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The quick stack, sort, bestiary and emote buttons at the top of FreshUI's side-button grid, where the
/// coin and ammo columns were: each is centred in its column (vanilla's areas, from x 498 and 534,
/// shifted; chest-open offsets kept), and the hotbar lock from the screen's left edge into the grid
/// below them (<see cref="SideButtons" />). Their areas
/// are lambdas, so they're wrapped once, when each button first updates.
/// </summary>
[Mixin(typeof(UIIconButtonScreen))]
public class UIIconButtonScreenMixin
{
    private bool _freshMoved;

    [Inject("UpdateState()", At.HEAD)]
    private void MoveOnce(CallbackInfo callback)
    {
        if (_freshMoved)
            return;
        _freshMoved = true;
        var screen = (UIIconButtonScreen)(object)this;
        if (screen == InventoryButtons.QuickStackAll || screen == InventoryButtons.Bestiary)
            SideButtons.Move(screen.Button, 498, SideButtons.ColumnX);
        else if (screen == InventoryButtons.SortInventory || screen == InventoryButtons.Emotes)
            SideButtons.Move(screen.Button, 534, SideButtons.ColumnX + SideButtons.Pitch);
        else if (screen == InventoryButtons.HotbarLock)
            SideButtons.PlaceHotbarLock(screen.Button);
    }
}
