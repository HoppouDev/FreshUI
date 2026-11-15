using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Chat;

namespace FreshUI.Hooks.Mixins;

/// <summary>
/// A button glyph in text (the gamepad's instruction bar, hints) centred on the text's capitals, not
/// hanging from the line's top: the glyph is 26 units tall at its scale, Andy Bold's capitals are
/// centred <see cref="SdfText.MouseTextCapMiddle"/> below the top at the text's (on whole UI pixels).
/// </summary>
[Mixin(typeof(GlyphTagHandler.GlyphSnippet))]
public static class GlyphSnippetMixin
{
    [ModifyArg("UniqueDraw(System.Boolean,Microsoft.Xna.Framework.Vector2&,Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Color,System.Single)",
        Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Vector2,System.Nullable`1<Microsoft.Xna.Framework.Rectangle>,Microsoft.Xna.Framework.Color,System.Single,Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Graphics.SpriteEffects,System.Single)",
        Index = 1)]
    private static Vector2 Position(Texture2D texture, Vector2 position, Rectangle? source, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth)
    {
        float textScale = scale / MathF.Max(0.01f, GlyphTagHandler.GlyphsScale);
        float lift = SdfText.MouseTextCapMiddle * textScale - 13f * scale;
        return position + new Vector2(0, MathF.Round(lift / 2) * 2);
    }
}
