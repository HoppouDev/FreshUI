using Aether.Modding;
using Terraria.GameContent.UI.BigProgressBar;

namespace FreshUI.Legacy.Mixins;

/// <summary>The boss health bar, above the bottom-centre hotbar while it's shown.</summary>
[Mixin(typeof(BigProgressBarHelper))]
public static class BigProgressBarHelperMixin
{
    [ModifyConstant("DrawBareBonesBar(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Single)", "float:-50")]
    private static float BareBonesBar(float offset) => offset - Hud.BossBarClearance;

    [ModifyConstant("DrawFancyBar(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Single,System.Single,Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Rectangle)", "float:-50")]
    private static float FancyBar(float offset) => offset - Hud.BossBarClearance;

    [ModifyConstant("DrawFancyBar(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Single,System.Single,Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Rectangle,System.Single,System.Single)", "float:-50")]
    private static float ShieldBar(float offset) => offset - Hud.BossBarClearance;
}
