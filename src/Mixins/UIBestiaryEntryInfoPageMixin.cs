using Aether.Modding;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Mixins;

/// <summary>The bestiary entry page's wheel scrolling sets its scrollbar gliding, a smaller step (<see cref="MomentumScroll" />).</summary>
[Mixin(typeof(UIBestiaryEntryInfoPage))]
public static class UIBestiaryEntryInfoPageMixin
{
    [Redirect("UpdateScrollbar(System.Int32)", At.INVOKE, Target = "Terraria.GameContent.UI.Elements.UIScrollbar::set_ViewPosition(System.Single)")]
    private static void Scroll(UIScrollbar scrollbar, float position) => MomentumScroll.Push(scrollbar, position);
}
