using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Legacy.Mixins;

/// <summary>An achievement's title and description strips and its category icon on whole UI pixels, as its panel and icon are, and its panel in the settings window's rows' colours.</summary>
[Mixin(typeof(UIAchievementListItem))]
public class UIAchievementListItemMixin
{
#pragma warning disable CS0649, CS8618
    [Shadow] private ReLogic.Content.Asset<Texture2D> _innerPanelTopTexture;
    [Shadow] private ReLogic.Content.Asset<Texture2D> _innerPanelBottomTexture;
#pragma warning restore CS0649, CS8618

    // The title and description boxes: flat strips on whole UI pixels, lit while hovered (the game's art,
    // single texels at the UI's fractional scale, can't draw evenly).
    [Inject("DrawPanelTop(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Color)", At.HEAD, Cancellable = true)]
    private void Top(SpriteBatch batch, Vector2 position, float width, Color color, CallbackInfo callback)
    {
        PanelArt.DrawStrip(batch, position, width, _innerPanelTopTexture.Height(), color == Color.White);
        callback.Cancel();
    }

    [Inject("DrawPanelBottom(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Color)", At.HEAD, Cancellable = true)]
    private void Bottom(SpriteBatch batch, Vector2 position, float width, Color color, CallbackInfo callback)
    {
        PanelArt.DrawStrip(batch, position, width, _innerPanelBottomTexture.Height(), color == Color.White);
        callback.Cancel();
    }

    // In the settings window's rows' colours: lit with a gold border while hovered.
    [Inject("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD)]
    private void Colors(CallbackInfo callback)
    {
        var item = (UIAchievementListItem)(object)this;
        PanelArt.LightRow(item, item.IsMouseHovering);
    }

    // The category icon.
    [Redirect("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.INVOKE,
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)")]
    private static void Icon(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        // At whole screen pixels per texel, centred where it was.
        Vector2 size = (source?.Size() ?? texture.Size());
        float whole = PanelArt.WholeTexels(scale);
        Vector2 at = PanelArt.Snap(position + size * (scale - whole) / 2);
        batch.Draw(texture, at, source, color, rotation, origin, whole, effects, depth);
    }
}
