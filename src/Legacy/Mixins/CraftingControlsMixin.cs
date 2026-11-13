using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Both the quick list and the crafting window use these shared control draw sites.</summary>
[Mixin(typeof(CraftingUI))]
public static class CraftingControlsMixin
{
    [Redirect("DrawGridToggle(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Int32,System.Int32,System.Int32)", At.INVOKE,
        Target = "Aether.UI.Game.CraftingToggles::DrawGridToggle(System.Int32,System.Int32,System.Int32)")]
    private static bool GridToggle(int x, int y, int gamepadPointId)
    {
        MoveToggle(ref x, ref y, 0);
        return CraftingToggles.DrawGridToggle(x, y, gamepadPointId);
    }

    [Redirect("DrawCraftFromNearbyChestsToggle(Microsoft.Xna.Framework.Graphics.SpriteBatch,System.Int32,System.Int32,System.Int32)", At.INVOKE,
        Target = "Aether.UI.Game.CraftingToggles::DrawNearbyToggle(System.Int32,System.Int32,System.Int32)")]
    private static bool NearbyToggle(int x, int y, int gamepadPointId)
    {
        MoveToggle(ref x, ref y, 1);
        return CraftingToggles.DrawNearbyToggle(x, y, gamepadPointId);
    }

    private static void MoveToggle(ref int x, ref int y, int row)
    {
        if (!HudScreen.TrySlotPosition(1, 50, out Vector2 coins))
            return;
        x = (int)MathF.Round(coins.X - 46 + 22);
        y = (int)MathF.Round(coins.Y + row * 46 + 22);
    }
}
