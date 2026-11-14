using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The player's buffs (Aether's <see cref="UIBuffBar"/>, the row at the top left, eleven to a line, and
/// <see cref="UIBuffColumns"/>, the columns under the misc equipment page): each buff's icon a Gum sprite with
/// its time left under it, brightening while hovered, cancelled by a right-click (with the gamepad, A in the
/// inventory), its name and tooltip shown while hovered, as the game's.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> BuffBarAsked = GumRoutes.GameScreen<UIBuffBar>();
    private static readonly Func<bool> BuffColumnsAsked = GumRoutes.GameScreen<UIBuffColumns>();
    private static readonly FieldInfo ColumnsX = typeof(UIBuffColumns).GetField("_x", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ColumnsY = typeof(UIBuffColumns).GetField("_y", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ColumnsPer = typeof(UIBuffColumns).GetField("_perColumn", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private const float BuffTop = 20;
    // Aether's frame time in 60ths of a second (internal), which the game's buff fading steps by.
    private static readonly Func<float> FrameScale = System.Linq.Expressions.Expression.Lambda<Func<float>>(System.Linq.Expressions.Expression.Property(null,
        typeof(Main).Assembly.GetType("Aether.Graphics.VisualAnimationTime")!.GetProperty("FrameScale", BindingFlags.NonPublic | BindingFlags.Static)!)).Compile();

    private readonly BuffIcons _barBuffs = new(), _columnBuffs = new();

    private void PresentBuffs()
    {
        Player player = Main.LocalPlayer;
        bool bar = IsReady && BuffBarAsked(), columns = IsReady && BuffColumnsAsked();
        int hovered = -1;
        if (bar)
        {
            Main.PipsUseGrid = false;
            hovered = _barBuffs.Present(this, columns: false, 32, (int)BuffTop, 11);
        }
        else
            _barBuffs.Hide();
        int columnHovered = -1;
        if (columns)
        {
            int x = (int)ColumnsX.GetValue(UIBuffColumns.Instance)!, y = (int)ColumnsY.GetValue(UIBuffColumns.Instance)!;
            int per = Math.Max(1, (int)ColumnsPer.GetValue(UIBuffColumns.Instance)!);
            columnHovered = _columnBuffs.Present(this, columns: true, x, y, per);
            UILinkPointNavigator.Shortcuts.BUFFS_DRAWN = _columnBuffs.Count;
            UILinkPointNavigator.Shortcuts.BUFFS_PER_COLUMN = per;
        }
        else
            _columnBuffs.Hide();
        // The hovered buff's name and tooltip (the bar's don't override other mouse text).
        int shown = columnHovered >= 0 ? columnHovered : hovered;
        if (shown < 0 || player.buffType[shown] <= 0)
            return;
        int type = player.buffType[shown];
        if (type == BuffID.MonsterBanner)
            Main.bannerMouseOver = true;
        string name = Lang.GetBuffName(type), tooltip = Main.GetBuffTooltip(player, type);
        bool noOverride = columnHovered < 0;
        if (Main.meleeBuff[type])
            Main.instance.MouseTextHackZoom(name, -10, 0, tooltip, noOverride);
        else
            Main.instance.MouseTextHackZoom(name, tooltip, noOverride);
    }

    /// <summary>A list of buff icons, one per buff slot, placed by the n-th active buff.</summary>
    private sealed class BuffIcons
    {
        private readonly SpriteRuntime[] _icons = new SpriteRuntime[Player.maxBuffs];
        private readonly TextRuntime[] _times = new TextRuntime[Player.maxBuffs];
        private readonly int[] _seconds = Enumerable.Repeat(-1, Player.maxBuffs).ToArray();
        private readonly GameCulture?[] _cultures = new GameCulture?[Player.maxBuffs];
        private bool _made;

        /// <summary>How many buffs are shown.</summary>
        public int Count { get; private set; }

        public void Hide()
        {
            if (!_made)
                return;
            for (int i = 0; i < Player.maxBuffs; i++)
            {
                if (_icons[i].Visible)
                {
                    _icons[i].Visible = false;
                    _times[i].Visible = false;
                }
            }
            Count = 0;
        }

        // Shows the active buffs, the n-th in a row of perLine from (x, y) (the bar), or in columns of perLine growing
        // leftwards from it (with gamepad points), in UI units; returns the hovered buff slot, or -1.
        public int Present(HudScreen screen, bool columns, int x, int y, int perLine)
        {
            bool gamepadPoints = columns;
            if (!_made)
                Make(screen);
            Player player = Main.LocalPlayer;
            int hovered = -1, index = 0;
            for (int i = 0; i < Player.maxBuffs; i++)
            {
                SpriteRuntime icon = _icons[i];
                TextRuntime time = _times[i];
                int type = player.buffType[i];
                if (type <= 0)
                {
                    if (!gamepadPoints)
                        Main.buffAlpha[i] = 0.4f;
                    if (icon.Visible)
                    {
                        icon.Visible = false;
                        time.Visible = false;
                    }
                    continue;
                }
                Vector2 at = columns ? new Vector2(x - index / perLine * 46, y + index % perLine * 46)
                    : new Vector2(x + index % perLine * 38, y + index / perLine * 50);
                Texture2D texture = TextureAssets.Buff[type].Value;
                bool over = Main.mouseX > at.X && Main.mouseX < at.X + texture.Width && Main.mouseY > at.Y && Main.mouseY < at.Y + texture.Height && !PlayerInput.IgnoreMouseInterface;
                if (over)
                {
                    Main.buffAlpha[i] += 0.1f * FrameScale();
                    bool cancel = Main.mouseRight && Main.mouseRightRelease;
                    if (PlayerInput.UsingGamepad)
                    {
                        cancel = Main.mouseLeft && Main.mouseLeftRelease && Main.playerInventory;
                        if (Main.playerInventory)
                            player.mouseInterface = true;
                    }
                    else
                        player.mouseInterface = true;
                    if (cancel)
                        Main.TryRemovingBuff(i, type);
                    if (!(PlayerInput.UsingGamepad && !Main.playerInventory))
                        hovered = i;
                }
                else
                    Main.buffAlpha[i] -= 0.05f * FrameScale();
                Main.buffAlpha[i] = MathHelper.Clamp(Main.buffAlpha[i], gamepadPoints ? 0.65f : 0.4f, 1f);
                if (gamepadPoints)
                    UILinkPointNavigator.SetPosition(9000 + index, at + new Vector2(30f));
                float alpha = Main.buffAlpha[i];
                var color = new Color(alpha, alpha, alpha, alpha);
                if (icon.Texture != texture)
                {
                    icon.Texture = texture;
                    icon.Width = texture.Width / 2f;
                    icon.Height = texture.Height / 2f;
                }
                if (icon.X != at.X / 2 || icon.Y != at.Y / 2)
                    (icon.X, icon.Y) = (at.X / 2, at.Y / 2);
                if (icon.Color != color)
                    icon.Color = color;
                if (!icon.Visible)
                    icon.Visible = true;
                Time(i, type, time, at + new Vector2(0, texture.Height), color);
                index++;
            }
            Count = index;
            return hovered;
        }

        // The time left (or the buff's own text) under its icon, made again only when the second or language changes.
        private void Time(int slot, int type, TextRuntime time, Vector2 at, Color color)
        {
            string? text = null;
            if (BuffID.Sets.BuffTextHandlers.TryGetValue(type, out var handler))
                text = handler.HandleBuffText();
            if (text == null && Main.TryGetBuffTime(slot, out int ticks) && ticks > 2)
            {
                int seconds = ticks / 60;
                if (seconds != _seconds[slot] || Language.ActiveCulture != _cultures[slot] || time.Text == null)
                {
                    (_seconds[slot], _cultures[slot]) = (seconds, Language.ActiveCulture);
                    time.Text = Lang.LocalizedDuration(new TimeSpan(0, 0, seconds), abbreviated: true, showAllAvailableUnits: false);
                }
                text = time.Text;
            }
            bool shown = text != null;
            if (time.Visible != shown)
                time.Visible = shown;
            if (!shown)
                return;
            if (time.Text != text)
            {
                time.Text = text;
                _seconds[slot] = -1;
            }
            if (time.X != at.X / 2 || time.Y != at.Y / 2)
                (time.X, time.Y) = (at.X / 2, at.Y / 2);
            if (time.Color != color)
                time.Color = color;
        }

        private void Make(HudScreen screen)
        {
            _made = true;
            for (int i = 0; i < Player.maxBuffs; i++)
            {
                _icons[i] = new SpriteRuntime
                {
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    Visible = false,
                };
                _times[i] = new TextRuntime
                {
                    WidthUnits = DimensionUnitType.RelativeToChildren,
                    HeightUnits = DimensionUnitType.RelativeToChildren,
                    Width = 0,
                    Height = 0,
                    FontSize = 7.5f,
                    HasEvents = false,
                    Visible = false,
                };
                GumText.SetOutline(_times[i], 0.5f, Color.Black);
                // Under the slots and the game's art over them.
                screen.Children.Insert(0, _icons[i]);
                screen.Children.Insert(1, _times[i]);
            }
        }
    }
}
