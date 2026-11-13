using Aether.Modding;
using Aether.UI.Game;

namespace FreshUI.Legacy.Mixins;

/// <summary>The buff rows in the top left corner, where the hotbar was.</summary>
[Mixin(typeof(UIBuffBar))]
public static class UIBuffBarMixin
{
    [ModifyConstant("Layout()", "int:76")]
    private static int Top(int top) => 20;
}
