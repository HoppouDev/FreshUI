using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Legacy.Mixins;

/// <summary>An achievement's title and description strips and its category icon on whole UI pixels, as its panel and icon are, and its panel in the settings window's rows' colours.</summary>
[Mixin(typeof(UIAchievementListItem))]
public class UIAchievementListItemMixin
{
    [ModifyArg("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", Target = "Terraria.GameContent.UI.Elements.UIAchievementListItem::DrawPanelTop(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Color)", Index = 1)]
    private static Vector2 TopPosition(Vector2 position) => PanelArt.Snap(position);

    [ModifyArg("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", Target = "Terraria.GameContent.UI.Elements.UIAchievementListItem::DrawPanelTop(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Color)", Index = 2)]
    private static float TopWidth(SpriteBatch batch, Vector2 position, float width, Color color) => PanelArt.SnapWidth(position.X, width);

    [ModifyArg("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", Target = "Terraria.GameContent.UI.Elements.UIAchievementListItem::DrawPanelBottom(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Color)", Index = 1)]
    private static Vector2 BottomPosition(Vector2 position) => PanelArt.Snap(position);

    [ModifyArg("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", Target = "Terraria.GameContent.UI.Elements.UIAchievementListItem::DrawPanelBottom(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Color)", Index = 2)]
    private static float BottomWidth(SpriteBatch batch, Vector2 position, float width, Color color) => PanelArt.SnapWidth(position.X, width);

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
    private static void Icon(SpriteBatch batch, Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth) =>
        batch.Draw(texture, PanelArt.Snap(position), source, color, rotation, origin, scale, effects, depth);
}
