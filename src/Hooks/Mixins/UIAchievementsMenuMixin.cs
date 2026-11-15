using Aether.Modding;
using FreshUI.Screens;
using Terraria.Achievements;
using Terraria.GameContent.UI.States;

namespace FreshUI.Hooks.Mixins;

/// <summary>The achievements menu (Gum's, <see cref="AchievementsScreen"/>): going to an achievement scrolls Gum's list to it.</summary>
[Mixin(typeof(UIAchievementsMenu))]
public class UIAchievementsMenuMixin
{
    [Inject("GotoAchievement(Terraria.Achievements.Achievement)", At.HEAD)]
    private void Goto(Achievement achievement, CallbackInfo callback) => AchievementsScreen.Goto(achievement);
}
