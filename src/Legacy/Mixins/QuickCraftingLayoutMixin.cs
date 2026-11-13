using Aether.Modding;
using Terraria;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Centre the quick-crafting list beside the inventory.</summary>
[Mixin(typeof(Main))]
public static class QuickCraftingLayoutMixin
{
    // DrawInventory computes the shared offset as (screenHeight - reference) / 2.
    // Using the sum of the list's top and bottom centres its complete visible extent.
    [ModifyConstant("DrawInventory()", "int:600")]
    [ModifyConstant("DrawInventory()", "int:508")]
    private static int CentreList(int reference)
    {
        if (!Main.InPipCrafting || Main.PipsUseGrid || Main.InGuideCraftMenu || Main.InReforgeMenu)
            return reference;
        float fraction = Main.screenHeight < 700 ? 200 : Main.screenHeight < 850 ? 225 : 250;
        int middle = (int)(Main.screenHeight / 600f * fraction);
        // The controls now flank the inventory; only the recipe list and ingredients need centring.
        float top = 380, bottom = 432;
        for (int i = 0; i < Main.numAvailableRecipes; i++)
        {
            // Centre the resting list, not its animated scroll offsets or fast-scroll scales.
            float offset = i * 65f;
            if (MathF.Abs(offset) > middle)
                break;
            float scale = MathF.Max(0.75f, 100 / (offset + 100));
            float y = 410 + offset * scale - 30 * scale;
            top = MathF.Min(top, y);
            bottom = MathF.Max(bottom, y + 52 * scale);
        }
        return (int)MathF.Round(top + bottom);
    }
}
