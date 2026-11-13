using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Coins in slots at whole pixels, like other item icons.</summary>
[Mixin(typeof(CoinSlot))]
public static class CoinSlotMixin
{
    [ModifyArg("DrawItemCoin(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Int32,System.Int32,System.Single,System.Single,Microsoft.Xna.Framework.Color,System.Single)",
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)",
        Index = 6)]
    private static float CoinScale(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        Rectangle frame = source ?? texture.Bounds;
        return PixelSlots.IconScale(scale, Math.Max(frame.Width, frame.Height));
    }
}
