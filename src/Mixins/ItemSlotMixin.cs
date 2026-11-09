using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace FreshUI.Mixins;

/// <summary>
/// Slots drawn at fractional scales: nine-sliced backgrounds, icons and the rest at whole pixels. Coin
/// and ammo slots have their own backgrounds (vanilla's are the inventory's), olive and brick red, so
/// their row under the inventory needs no labels.
/// </summary>
[Mixin(typeof(ItemSlot))]
public static class ItemSlotMixin
{
    private const string DrawSlot = "Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch,Terraria.Item[],System.Int32,System.Int32,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color)";

    private const string SpriteDraw = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)";

    // The context of the slot being drawn (-1, or 0 before the first: none that has its own background).
    private static int _context;

    [Inject(DrawSlot, At.HEAD)]
    private static void BeginSlot(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor, CallbackInfo callback) => _context = context;

    [Inject(DrawSlot, At.RETURN)]
    private static void EndSlot(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor, CallbackInfo callback) => _context = -1;

    [Redirect(DrawSlot, At.INVOKE, Target = SpriteDraw)]
    private static void DrawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        // The plain background only: a favourite's, a new item's and the rest keep theirs.
        if (texture == TextureAssets.InventoryBack.Value && (_context == 1 || _context == 2))
            texture = (_context == 1 ? TextureAssets.InventoryBack6 : TextureAssets.InventoryBack5).Value;
        PixelSlots.DrawSprite(spriteBatch, texture, position, source, color, rotation, origin, scale, effects, depth);
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
