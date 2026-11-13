using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Keep Aether's live slot handling and layout; present the item through Gum.</summary>
[Mixin(typeof(UIGameItemSlot))]
public class UIGameItemSlotMixin
{
#pragma warning disable CS0649
    [Shadow] private Action<Item>? _setItem;
    [Shadow] public Func<bool>? BeforeHandle;
#pragma warning restore CS0649

    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Terraria.UI.ItemSlot::Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch,Terraria.Item[],System.Int32,System.Int32,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color)")]
    private void DrawSlot(SpriteBatch batch, Item[] items, int context, int index, Vector2 position, Color color)
    {
        if (!HudScreen.Present(this, items, context, index, position, _setItem, BeforeHandle))
            ItemSlot.Draw(batch, items, context, index, position, color);
    }
}
