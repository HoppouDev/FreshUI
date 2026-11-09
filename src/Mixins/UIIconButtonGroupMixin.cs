using Aether.Modding;
using Aether.UI.Game;

namespace FreshUI.Mixins;

/// <summary>
/// The equipment page's icons (camera, housing, equipment) each centred over their column of the
/// page, which FreshUI moved onto a 46-unit pitch (vanilla's icons are 48 apart, from the left).
/// Their areas are lambdas, so they're wrapped once, when the group first draws.
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
        if (group != InventoryIcons.PageIcons)
            return;
        foreach (UIIconButton button in group.Buttons)
            PageIcons.Centre(button);
    }
}
