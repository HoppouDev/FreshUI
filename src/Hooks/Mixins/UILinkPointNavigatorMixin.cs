using Aether.Modding;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI.Gamepad;

namespace FreshUI.Hooks.Mixins;

/// <summary>Records which gamepad points are placed each update: the HUD's focus targets (<see cref="GumHudFocus"/>).</summary>
[Mixin(typeof(UILinkPointNavigator))]
public static class UILinkPointNavigatorMixin
{
    // The game's, recording the point: an overwrite, not an inject, as it's called for every point every frame (an
    // inject's callback is an allocation each).
    [Overwrite]
    public static void SetPosition(int ID, Vector2 Position)
    {
        UILinkPointNavigator.Points[ID].Position = Position * Main.UIScale;
        GumHudFocus.Place(ID);
    }
}
