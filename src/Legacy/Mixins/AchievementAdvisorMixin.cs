using Aether.Modding;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>The achievement advisor's card at whole pixels: it's a frame and an icon drawn at 0.75 (or 0.35).</summary>
[Mixin(typeof(AchievementAdvisor))]
public static class AchievementAdvisorMixin
{
    [ModifyConstant("DrawOneAchievement(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Boolean)", "float:0.75")]
    private static float LargeScale(float scale) => Sharp.WholeScale(scale);

    [ModifyConstant("DrawOneAchievement(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Boolean)", "float:0.35")]
    private static float SmallScale(float scale) => Sharp.WholeScale(scale);
}
