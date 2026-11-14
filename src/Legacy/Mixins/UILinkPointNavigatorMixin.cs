using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria.UI.Gamepad;

namespace FreshUI.Legacy.Mixins;

/// <summary>Records which gamepad points are drawn each frame (<see cref="InventoryNavigation"/>).</summary>
[Mixin(typeof(UILinkPointNavigator))]
public static class UILinkPointNavigatorMixin
{
    [Inject("SetPosition(System.Int32,Microsoft.Xna.Framework.Vector2)", At.HEAD)]
    private static void Placed(int id, Vector2 position, CallbackInfo callback) => InventoryNavigation.Place(id);
}
