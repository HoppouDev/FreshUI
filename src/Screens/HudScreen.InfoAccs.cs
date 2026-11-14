using Aether.UI.Game;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The info accessories' readouts (Aether's <see cref="UIInfoAccs"/>: the game works out each row, a watch's
/// time, the depth…, and hands it over): each row's icon a Gum sprite and its text Gum text in the row's
/// colours, under the minimap; with the inventory open the icons line up, a click hiding or showing a row
/// (hidden ones dimmed), and hovering an icon names it.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> InfoAccsAsked = GumRoutes.GameScreen<UIInfoAccs>();
    private static readonly Func<UIInfoAccs, List<UIInfoAccRow>> InfoRows = Member<UIInfoAccs, List<UIInfoAccRow>>(typeof(UIInfoAccs), "_rows");
    private static readonly Func<UIInfoAccs, int> InfoCount = Member<UIInfoAccs, int>(typeof(UIInfoAccs), "_count");
    private static readonly Func<UIInfoAccs, int> InfoStartX = Member<UIInfoAccs, int>(typeof(UIInfoAccs), "_startX");
    private static readonly Func<UIInfoAccs, float> InfoMaxWidth = Member<UIInfoAccs, float>(typeof(UIInfoAccs), "_maxWidth");
    private static readonly Func<UIInfoAccRow, int> RowLine = Member<UIInfoAccRow, int>(typeof(UIInfoAccRow), "_line");
    private static readonly Func<UIInfoAccRow, int> RowIcon = Member<UIInfoAccRow, int>(typeof(UIInfoAccRow), "_icon");
    private static readonly Func<UIInfoAccRow, string> RowText = Member<UIInfoAccRow, string>(typeof(UIInfoAccRow), "_text");
    private static readonly Func<UIInfoAccRow, string> RowName = Member<UIInfoAccRow, string>(typeof(UIInfoAccRow), "_name");
    private static readonly Func<UIInfoAccRow, Color> RowTextColor = Member<UIInfoAccRow, Color>(typeof(UIInfoAccRow), "_textColor");
    private static readonly Func<UIInfoAccRow, Color> RowShadowColor = Member<UIInfoAccRow, Color>(typeof(UIInfoAccRow), "_shadowColor");

    private delegate void InfoIconPosition(int drawnCount, int startX, out int x, out int y);
    private static readonly InfoIconPosition IconPosition = (InfoIconPosition)Delegate.CreateDelegate(typeof(InfoIconPosition),
        typeof(Main).GetMethod("GetInfoAccIconPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!);

    private readonly List<(SpriteRuntime Icon, SpriteRuntime Light, TextRuntime Text)> _infoRows = [];

    private void PresentInfoAccs()
    {
        int count = IsReady && InfoAccsAsked() ? InfoCount(UIInfoAccs.Instance) : 0;
        List<UIInfoAccRow> rows = InfoRows(UIInfoAccs.Instance);
        Player player = Main.LocalPlayer;
        int startX = InfoStartX(UIInfoAccs.Instance), drawn = 0, state = -1, hoveredLine = -10;
        float maxWidth = InfoMaxWidth(UIInfoAccs.Instance);
        int rowStep = Main.screenHeight < 650 ? 20 : 22;
        string? hoverText = null;
        for (int i = 0; i < count; i++)
        {
            if (i == _infoRows.Count)
                _infoRows.Add(MakeInfoRow());
            (SpriteRuntime iconSprite, SpriteRuntime light, TextRuntime text) = _infoRows[i];
            UIInfoAccRow row = rows[i];
            int line = RowLine(row), icon = RowIcon(row);
            IconPosition(drawn, startX, out int x, out int y);
            bool lit = false;
            if (icon >= 0)
            {
                drawn++;
                Texture2D texture = TextureAssets.InfoIcon[icon].Value;
                Vector2 at = Main.playerInventory ? new Vector2(x, y) : new Vector2(x, y + 74 + rowStep * line + 52);
                int hideIndex = icon == 8 ? 7 : icon;
                bool inside = Main.mouseX >= at.X && Main.mouseY >= at.Y && Main.mouseX <= at.X + texture.Width && Main.mouseY <= at.Y + texture.Height;
                Color color = Color.White;
                if (Main.playerInventory)
                {
                    if (inside)
                    {
                        state = drawn;
                        if (!PlayerInput.IgnoreMouseInterface)
                        {
                            lit = true;
                            player.mouseInterface = true;
                            if (Main.mouseLeft && Main.mouseLeftRelease)
                            {
                                SoundEngine.PlaySound(12);
                                Main.mouseLeftRelease = false;
                                player.hideInfo[hideIndex] = !player.hideInfo[hideIndex];
                            }
                            hoverText ??= RowName(row);
                        }
                    }
                    if (player.hideInfo[hideIndex])
                        color = new Color(80, 80, 80, 70);
                }
                else if (inside && hoverText == null)
                {
                    hoveredLine = line;
                    hoverText = RowName(row);
                }
                UILinkPointNavigator.SetPosition(1558 + drawn - 1, at + texture.Size() * 0.75f);
                Place(iconSprite, texture, at);
                if (iconSprite.Color != color)
                    iconSprite.Color = color;
                if (light.Visible != lit)
                    light.Visible = lit;
                if (lit)
                {
                    Place(light, TextureAssets.InfoIcon[13].Value, at - Vector2.One * 2f);
                    if (light.Color != Main.OurFavoriteColor)
                        light.Color = Main.OurFavoriteColor;
                }
                x += 20;
            }
            else if (iconSprite.Visible)
            {
                iconSprite.Visible = false;
                light.Visible = false;
            }
            UILinkPointNavigator.Shortcuts.INFOACCCOUNT = drawn;
            InfoText(text, Main.playerInventory ? null : RowText(row), new Vector2(x, y + 74 + rowStep * line + 48), maxWidth,
                RowTextColor(row), RowShadowColor(row), line > hoveredLine && line < hoveredLine + 2);
        }
        for (int i = count; i < _infoRows.Count; i++)
        {
            (SpriteRuntime iconSprite, SpriteRuntime light, TextRuntime text) = _infoRows[i];
            if (iconSprite.Visible || text.Visible || light.Visible)
                iconSprite.Visible = light.Visible = text.Visible = false;
        }
        if (hoverText != null)
        {
            if (Main.playerInventory)
                player.mouseInterface = true;
            Main.instance.MouseTextNoOverride(hoverText, 0, 0);
        }
        Main.DoStatefulTickSound(ref Main.infoAccMouseOver, state);
    }

    private (SpriteRuntime, SpriteRuntime, TextRuntime) MakeInfoRow()
    {
        SpriteRuntime icon = Sprite(), light = Sprite();
        light.Visible = false;
        var text = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 10,
            HasEvents = false,
            Visible = false,
        };
        Children.Insert(0, text);
        return (icon, light, text);
    }

    // A row's text in its colour, outlined in its shadow colour (dimmed under a hovered icon so its name reads),
    // smaller where it's wider than the room.
    private static void InfoText(TextRuntime text, string? value, Vector2 at, float maxWidth, Color color, Color shadow, bool dim)
    {
        bool shown = !string.IsNullOrEmpty(value);
        if (text.Visible != shown)
            text.Visible = shown;
        if (!shown)
            return;
        if (text.Text != value)
        {
            text.Text = value;
            text.FontSize = 10;
            float width = text.GetAbsoluteWidth() * 2;
            if (width > maxWidth)
                text.FontSize = MathF.Max(6, 10 * maxWidth / width);
        }
        if (dim)
        {
            color = new Color(color.R / 3, color.G / 3, color.B / 3, color.A / 3);
            shadow = new Color(shadow.R / 3, shadow.G / 3, shadow.B / 3, shadow.A / 3);
        }
        if (text.Color != color)
            text.Color = color;
        if (GumText.GetOutline(text) != (0.5f, shadow))
            GumText.SetOutline(text, 0.5f, shadow);
        float x = MathF.Round(at.X / 2), y = MathF.Round(at.Y / 2);
        if (text.X != x || text.Y != y)
            (text.X, text.Y) = (x, y);
    }
}
