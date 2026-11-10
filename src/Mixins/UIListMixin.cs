using Aether.Modding;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Mixins;

/// <summary>A list's wheel scrolling sets its scrollbar gliding, a smaller step (<see cref="MomentumScroll" />).</summary>
[Mixin(typeof(UIList))]
public static class UIListMixin
{
    [Redirect("ScrollWheel(Terraria.UI.UIScrollWheelEvent)", At.INVOKE, Target = "Terraria.GameContent.UI.Elements.UIScrollbar::set_ViewPosition(System.Single)")]
    private static void Scroll(UIScrollbar scrollbar, float position) => MomentumScroll.Push(scrollbar, position);
}
