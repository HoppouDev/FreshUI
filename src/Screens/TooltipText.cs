using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.UI.Chat;

namespace FreshUI.Screens;

/// <summary>
/// Hover text and tooltips (an item's, a buff's, a name under the cursor) in Gum's text: the game still works
/// out every line, its colour and where it goes (and draws its box, a <c>FreshPanel</c>), and <c>MouseTextMixin</c>
/// hands each line here in place of drawing it; at the tooltip's end they're drawn on the spot in the game's
/// batch (<see cref="GumInline{T}"/>), as Andy Bold at the game's size, placed on its baseline. A line with chat
/// tags (an item's icon, a colour tag) is the game's to draw.
/// </summary>
public static class TooltipText
{
    // The game's mouse text: 19 units an em (Gum's 9.5 pixels), its baseline 15.75 units below a line's top (SdfText).
    private const float FontSize = 9.5f, Baseline = 15.75f;

    private static readonly GumInline<ContainerRuntime> Lines = new(() => new ContainerRuntime { HasEvents = false });
    private static readonly List<TextRuntime> Pool = [];
    private static int _used;

    /// <summary>A line of the tooltip, as <c>ChatManager.DrawColorCodedStringWithShadow</c> would draw it.</summary>
    public static void Line(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation, Vector2 origin, Vector2 scale, float maxWidth, float spread)
    {
        if (!GumHost.IsInitialized || font != Terraria.GameContent.FontAssets.MouseText.Value || rotation != 0 || origin != Vector2.Zero || scale != Vector2.One ||
            maxWidth > 0 || text.Contains('[') || GumHost.BatchTransform(batch) != Main.UIScaleMatrix)
        {
            ChatManager.DrawColorCodedStringWithShadow(batch, font, text, position, color, rotation, origin, scale, maxWidth, spread);
            return;
        }
        Add(text, position, color);
    }

    /// <summary>A line the game draws outlined by hand (a buff's tooltip): only its own pass, not the four shadows.</summary>
    public static void OutlinedLine(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation, Vector2 origin, float scale, SpriteEffects effects, float depth, Vector2[] offsets, Color[] colors)
    {
        if (!GumHost.IsInitialized || font != Terraria.GameContent.FontAssets.MouseText.Value || scale != 1 || text.Contains('[') ||
            GumHost.BatchTransform(batch) != Main.UIScaleMatrix)
        {
            batch.DrawString(font, text, position, color, rotation, origin, scale, effects, depth, offsets, colors);
            return;
        }
        if (color.R != 0 || color.G != 0 || color.B != 0)
            Add(text, position, color);
    }

    private static void Add(string text, Vector2 position, Color color)
    {
        if (_used == Pool.Count)
        {
            var created = new TextRuntime
            {
                WidthUnits = DimensionUnitType.RelativeToChildren,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                FontSize = FontSize,
                HasEvents = false,
            };
            GumText.SetOutline(created, 1, Color.Black);
            Pool.Add(created);
            Lines.Element.Children.Add(created);
        }
        TextRuntime line = Pool[_used++];
        if (line.Text != text)
            line.Text = text;
        float x = MathF.Round(position.X / 2), y = MathF.Round((position.Y + Baseline) / 2 - FontSize * GumText.Baseline);
        if (line.X != x || line.Y != y)
            (line.X, line.Y) = (x, y);
        if (line.Color != color)
            line.Color = color;
        if (!line.Visible)
            line.Visible = true;
    }

    /// <summary>The tooltip's end: draws its lines, over what the game drew of it.</summary>
    public static void Flush(SpriteBatch batch)
    {
        if (_used == 0)
            return;
        for (int i = _used; i < Pool.Count; i++)
        {
            if (Pool[i].Visible)
                Pool[i].Visible = false;
        }
        _used = 0;
        Lines.Draw(batch, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight));
    }
}
