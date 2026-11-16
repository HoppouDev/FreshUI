using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Terraria.UI;

namespace FreshUI.Hooks.Mixins;

/// <summary>The banners' toggle (shown while there are banners to claim) under the crafting window's, right of the ammo.</summary>
[Mixin(typeof(BannerClaimingUI))]
public static class BannerClaimingUIMixin
{
    [Redirect("DrawGridToggle(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Int32)", At.INVOKE,
        Target = "Aether.UI.Game.UIBannerList::DrawToggle(System.Int32,System.Int32)")]
    private static bool Toggle(int x, int y)
    {
        SideButtons.BannerToggleShown = true;
        SideButtons.MoveToggle(ref x, ref y, 0, 1);
        return UIBannerList.DrawToggle(x, y);
    }
}
