using Aether.Modding;
using Aether.UI.Game;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The equipment page's icons (camera, housing, equipment) each centred over their column of the
/// page, which FreshUI moved onto a 46-unit pitch (vanilla's icons are 48 apart, from the left).
/// The builder toggles (block replacement, torch swapping, the builder accessories') move from the
/// screen's left edge into the inventory's side-button grid (<see cref="SideButtons" />). Their areas
/// are lambdas, so they're wrapped once, when the group first draws.
/// </summary>
[Mixin(typeof(UIIconButtonGroup))]
public class UIIconButtonGroupMixin
{
    private bool _freshMoved;

    [Inject("Draw()", At.HEAD)]
    private void MoveOnce(CallbackInfoReturnable<bool> callback)
    {
        if (_freshMoved)
            return;
        _freshMoved = true;
        var group = (UIIconButtonGroup)(object)this;
        if (group == InventoryIcons.PageIcons)
        {
            foreach (UIIconButton button in group.Buttons)
                PageIcons.Centre(button);
        }
        else if (group == UIBuilderToggles.Instance)
            SideButtons.PlaceBuilderToggles(UIBuilderToggles.Instance);
    }
}
