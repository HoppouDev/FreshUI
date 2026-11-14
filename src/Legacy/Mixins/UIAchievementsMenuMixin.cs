using Aether.Modding;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>The achievements menu's title at the settings window's size (the game's is larger than its other menus').</summary>
[Mixin(typeof(UIAchievementsMenu))]
public class UIAchievementsMenuMixin
{
    [Inject("InitializePage()", At.TAIL)]
    private void TitleSize(CallbackInfo callback) => PanelArt.TitleSize((UIElement)(object)this, "UI.Achievements");
}
