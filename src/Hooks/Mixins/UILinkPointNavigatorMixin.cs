using Aether.Modding;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Terraria.UI.Gamepad;

namespace FreshUI.Hooks.Mixins;

/// <summary>Records which gamepad points are placed each update: the HUD's focus targets (<see cref="GumHudFocus"/>).</summary>
[Mixin(typeof(UILinkPointNavigator))]
public static class UILinkPointNavigatorMixin
{
    [Inject("SetPosition(System.Int32,Microsoft.Xna.Framework.Vector2)", At.HEAD)]
    private static void Placed(int id, Vector2 position, CallbackInfo callback) => GumHudFocus.Place(id);
}
