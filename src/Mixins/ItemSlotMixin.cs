using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace FreshUI.Mixins;

/// <summary>Slots drawn at fractional scales: nine-sliced backgrounds, icons at whole pixels.</summary>
[Mixin(typeof(ItemSlot))]
public static class ItemSlotMixin
{
    private const string DrawSlot = "Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch,Terraria.Item[],System.Int32,System.Int32,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color)";

    private const string SpriteDraw = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)";

    [Redirect(DrawSlot, At.INVOKE, Target = SpriteDraw)]
    private static void DrawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        if (PixelSlots.IsSlotBackground(texture, source, rotation, origin))
            PixelSlots.DrawSlot(spriteBatch, texture, position, color, scale);
        else
            spriteBatch.Draw(texture, position, source, color, rotation, origin, scale, effects, depth);
    }

    // The slot's icon (drawn at the slot's scale; the glow behind a favourite is larger) knows its slot.
    [Redirect(DrawSlot, At.INVOKE, Target = "Terraria.UI.ItemSlot::DrawItemIcon(Terraria.Item,System.Int32,Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,System.Single,Microsoft.Xna.Framework.Color,System.Single,System.Boolean)")]
    private static float DrawIcon(Item item, int context, SpriteBatch spriteBatch, Vector2 center, float scale, float sizeLimit, Color color, float fade, bool flip)
    {
        if (scale != Main.inventoryScale)
            return ItemSlot.DrawItemIcon(item, context, spriteBatch, center, scale, sizeLimit, color, fade, flip);
        PixelSlots.Slot = 52f * Main.inventoryScale;
        try
        {
            return ItemSlot.DrawItemIcon(item, context, spriteBatch, center, scale, sizeLimit, color, fade, flip);
        }
        finally
        {
            PixelSlots.Slot = 0f;
        }
    }

    [Redirect(DrawSlot, At.INVOKE, Target = "Terraria.UI.CoinSlot::DrawItemCoin(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Int32,System.Int32,System.Single,System.Single,Microsoft.Xna.Framework.Color,System.Single)")]
    private static float DrawCoin(SpriteBatch spriteBatch, Vector2 center, int coinType, int coinFrame, float scale, float sizeLimit, Color color, float fade)
    {
        if (scale != Main.inventoryScale)
            return CoinSlot.DrawItemCoin(spriteBatch, center, coinType, coinFrame, scale, sizeLimit, color, fade);
        PixelSlots.Slot = 52f * Main.inventoryScale;
        try
        {
            return CoinSlot.DrawItemCoin(spriteBatch, center, coinType, coinFrame, scale, sizeLimit, color, fade);
        }
        finally
        {
            PixelSlots.Slot = 0f;
        }
    }

    // Vanilla's, with the icon's scale put on whole pixels in a slot.
    [Overwrite]
    public static void DrawItem_GetColorAndScale(Item item, float scale, ref Color currentWhite, float sizeLimit, ref Rectangle frame, out Color itemLight, out float finalDrawScale)
    {
        itemLight = currentWhite;
        float lightScale = 1f;
        ItemSlot.GetItemLight(ref itemLight, ref lightScale, item);
        float fit = 1f;
        if (frame.Width > sizeLimit || frame.Height > sizeLimit)
            fit = frame.Width <= frame.Height ? sizeLimit / frame.Height : sizeLimit / frame.Width;
        if (item.type == 5669 && sizeLimit == 20f)
            fit = 0.5f;
        finalDrawScale = PixelSlots.IconScale(scale * fit, Math.Max(frame.Width, frame.Height)) * lightScale;
    }
}
