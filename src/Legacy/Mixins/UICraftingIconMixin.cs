using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Recipe results and ingredients use Gum slots; the crafting lists keep their own actions.</summary>
[Mixin(typeof(UICraftingIcon))]
public class UICraftingIconMixin
{
    private Item[]? _freshItems;

    [Redirect("DrawItem(Microsoft.Xna.Framework.Graphics.SpriteBatch,Terraria.Item&,Microsoft.Xna.Framework.Vector2,System.Double,Microsoft.Xna.Framework.Color,System.Int32)", At.INVOKE,
        Target = "Terraria.UI.ItemSlot::Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch,Terraria.Item&,System.Int32,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color)")]
    private void DrawSlot(SpriteBatch batch, ref Item item, int context, Vector2 position, Color color)
    {
        _freshItems ??= new Item[1];
        _freshItems[0] = item;
        if (!HudScreen.Present(this, _freshItems, context, 0, position, interactive: false))
            ItemSlot.Draw(batch, ref item, context, position, color);
    }
}
