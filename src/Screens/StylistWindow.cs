using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The Stylist's hair window (Aether's <see cref="UIStylistWindow"/>, the game's <c>Main.DrawHairWindow</c>) in
/// FreshUI's style: a <see cref="FreshPanel"/> where the game's art was, with the hair colour's
/// <see cref="FreshColorPicker"/>, Buy (with its price, the player's savings above it) and Cancel, and fifteen
/// hairstyles at a time as <see cref="FreshPanelButton"/>s (the current one chosen), each with a bald head
/// wearing it in the chosen colour (drawn by the game), paged by the arrows or the wheel.
/// </summary>
internal sealed class StylistWindow : ContainerRuntime
{
    private static readonly Func<bool> Asked = GumRoutes.GameScreen<UIStylistWindow>();
    private const int Columns = 5, Shown = 15;
    private const float Cell = 26, CellPitch = 28;

    private readonly FreshColorPicker _colors = new() { MinLightness = 0.15f, X = 11, Y = 21 };
    private readonly FreshValueButton _buy = new(), _cancel = new(), _previous = new() { Text = "<" }, _next = new() { Text = ">" };
    private readonly FreshPanelButton[] _styles = new FreshPanelButton[Shown];
    private readonly FreshGameView _art;
    private int _price;

    public StylistWindow()
    {
        HasEvents = false;
        Visible = false;
        var panel = new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, HeightUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = 0 };
        Children.Add(panel);
        _colors.Get = () => new Vector3(Main.hBar, Main.sBar, Main.lBar);
        _colors.Set = hsl =>
        {
            (Main.hBar, Main.sBar, Main.lBar) = (hsl.X, hsl.Y, hsl.Z);
            Main.selColor = Main.hslToRgb(hsl.X, hsl.Y, hsl.Z);
            Main.LocalPlayer.hairColor = Main.selColor;
        };
        Children.Add(_colors);
        foreach (FreshValueButton button in new[] { _buy, _cancel, _previous, _next })
        {
            // The game's choice colour (its text's yellow).
            button.Color = new Color(255, 231, 128);
            Children.Add(button.Visual);
        }
        _buy.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Buy(); };
        _cancel.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Main.CancelHairWindow(); };
        _previous.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Page(-Shown); };
        _next.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Page(Shown); };
        for (int i = 0; i < Shown; i++)
        {
            int index = i;
            var style = new FreshPanelButton { Text = "" };
            style.Visual.WidthUnits = style.Visual.HeightUnits = DimensionUnitType.Absolute;
            style.Visual.Width = style.Visual.Height = Cell;
            style.Click += (_, _) => { if (!PlayerInput.UsingGamepadUI) Choose(index); };
            _styles[i] = style;
            Children.Add(style.Visual);
        }
        _art = new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = DrawArt,
        };
        Children.Add(_art);
    }

    private static Texture2D Back => TextureAssets.HairStyleBack.Value;

    private const float StylesLeft = 117;

    /// <summary>Each frame, at draw time.</summary>
    public void Present()
    {
        bool shown = HudScreen.IsReady && Asked();
        if (Visible != shown)
            Visible = shown;
        if (!shown)
            return;
        Player player = Main.LocalPlayer;
        Main.Hairstyles.UpdateUnlocks();
        float width = MathF.Round(Back.Width / 2f), height = MathF.Round(Back.Height / 2f);
        float x = MathF.Round((GraphicalUiElement.CanvasWidth - width) / 2), y = MathF.Round(GraphicalUiElement.CanvasHeight / 2 + 30);
        if (X != x || Y != y || Width != width || Height != height)
            (X, Y, Width, Height) = (x, y, width, height);
        Rectangle area = HudPoints.Units(this);
        Main.selColor = player.hairColor;
        // Opening the window resets the bars to -1; they start from the hair's colour then.
        if (Main.hBar == -1f || Main.sBar == -1f || Main.lBar == -1f)
        {
            Vector3 hsl = Main.rgbToHsl(Main.selColor);
            (Main.hBar, Main.sBar, Main.lBar) = (hsl.X, hsl.Y, hsl.Z);
        }
        _colors.Sync();
        // The wheel turns a row of styles at a time.
        if (area.Contains(Main.mouseX, Main.mouseY))
        {
            Main.LocalPlayer.mouseInterface = true;
            for (int steps = -PlayerInput.ScrollWheelDelta / 120; steps != 0; steps -= Math.Sign(steps))
            {
                Main.hairStart += Columns * Math.Sign(steps);
                SoundEngine.PlaySound(12);
            }
        }
        int count = Main.Hairstyles.AvailableHairstyles.Count;
        Main.hairStart = Math.Max(0, Math.Min(Main.hairStart, count - Shown));
        _price = Price();
        for (int row = 0; row < 3; row++)
        {
            HudPoints.Place(_colors.Bars[row].Visual, 2600 + row, out bool focused);
            // The game's gamepad moves a focused colour bar.
            if (focused)
                UILinkPointNavigator.Shortcuts.OPTIONS_BUTTON_SPECIALFEATURE = 5 + row;
        }
        if (!Main.mouseLeft)
            Main.blockMouse = false;

        SetText(_buy, PriceText(_price));
        _buy.IsEnabled = _price > 0;
        SetText(_cancel, Language.GetTextValue("GameUI.Cancel"));
        Place(_buy, 9, 78);
        Place(_cancel, 74, 78);
        Main.npcChatFocus1 = Choice(_buy, 2603, Buy);
        Main.npcChatFocus2 = Choice(_cancel, 2604, () => Main.CancelHairWindow());

        bool canPrevious = Main.hairStart >= 1, canNext = Main.hairStart + Shown < count;
        _previous.Visual.Visible = canPrevious;
        _next.Visual.Visible = canNext;
        Place(_previous, StylesLeft - 9, 9 + CellPitch + 4);
        Place(_next, StylesLeft + Columns * CellPitch + 2, 9 + CellPitch + 4);
        for (int i = 0; i < Shown; i++)
        {
            FreshPanelButton cell = _styles[i];
            float cellX = StylesLeft + i % Columns * CellPitch, cellY = 9 + i / Columns * CellPitch;
            if (cell.Visual.X != cellX || cell.Visual.Y != cellY)
                (cell.Visual.X, cell.Visual.Y) = (cellX, cellY);
            int style = Main.Hairstyles.AvailableHairstyles[Main.hairStart + i];
            cell.IsChosen = player.hair == style;
            if (HudPoints.Place(cell.Visual, 2605 + i, out bool focused))
                Choose(i);
            cell.IsLit = focused;
        }
    }

    private static void SetText(FreshValueButton button, string text)
    {
        if (button.Text != text)
            button.Text = text;
    }

    private static void Place(FreshValueButton button, float x, float y)
    {
        if (button.Visual.X != x || button.Visual.Y != y)
            (button.Visual.X, button.Visual.Y) = (x, y);
    }

    // A choice on the gamepad's point (A acts); true while hovered or focused (the game's focus fields).
    private static bool Choice(FreshValueButton button, int point, Action act)
    {
        if (!button.IsEnabled)
        {
            HudPoints.Place(button.Visual, point, out _);
            button.IsLit = false;
            return false;
        }
        if (HudPoints.Place(button.Visual, point, out bool focused))
            act();
        button.IsLit = focused;
        return focused || HudPoints.Hovered(button.Visual);
    }

    private void Buy()
    {
        if (_price > 0 && Main.LocalPlayer.BuyItem(_price))
            Main.BuyHairWindow();
    }

    private static void Page(int by)
    {
        Main.hairStart += by;
        SoundEngine.PlaySound(12);
    }

    private static void Choose(int index)
    {
        int at = Main.hairStart + index;
        if (at >= Main.Hairstyles.AvailableHairstyles.Count)
            return;
        Main.LocalPlayer.hair = Main.Hairstyles.AvailableHairstyles[at];
        SoundEngine.PlaySound(12);
    }

    private static readonly Func<int> OldHairStyle = MainField<int>("oldHairStyle");
    private static readonly Func<Color> OldHairColor = MainField<Color>("oldHairColor");

    /// <summary>A static field of the game's <see cref="Main"/> that isn't public, as a getter.</summary>
    internal static Func<T> MainField<T>(string name) => System.Linq.Expressions.Expression.Lambda<Func<T>>(
        System.Linq.Expressions.Expression.Field(null, typeof(Main).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!)).Compile();

    private int _pricedAt = -1;
    private GameCulture? _pricedIn;
    private string _priceText = "";

    // "Buy" or "Buy (10 gold)", made again only when the price or language changes.
    private string PriceText(int price)
    {
        if (price == _pricedAt && Language.ActiveCulture == _pricedIn)
            return _priceText;
        (_pricedAt, _pricedIn) = (price, Language.ActiveCulture);
        if (price == 0)
            return _priceText = Language.GetTextValue("GameUI.Buy");
        string coins = "";
        int[] split = Utils.CoinsSplit(price);
        for (int i = 3; i >= 0; i--)
        {
            if (split[i] > 0)
                coins += split[i] + " " + Lang.inter[15 + (3 - i)].Value + " ";
        }
        return _priceText = Language.GetTextValue("GameUI.BuyWithValue", coins.TrimEnd());
    }

    // As the game's: 10 gold for a new style, 2 for a new colour, with the usual discounts, rounded to gold.
    private static int Price()
    {
        Player player = Main.LocalPlayer;
        int price = (OldHairStyle() != player.hair ? 100000 : 0) + (OldHairColor() != player.hairColor ? 20000 : 0);
        if (player.discountAvailable)
            price = (int)(price * 0.8f);
        price = (int)(price * player.currentShoppingSettings.PriceAdjustment);
        return Math.Max(0, (int)Math.Round(price / 10000f) * 10000);
    }

    // The game's art: the savings over Buy, and a bald head in each style's cell wearing it, in the chosen colour.
    private void DrawArt(SpriteBatch batch, Rectangle area)
    {
        Player player = Main.LocalPlayer;
        if (_price > 0)
            ItemSlot.DrawSavings(batch, area.X + 18, area.Y + 156 - 70, horizontal: true);
        int count = Main.Hairstyles.AvailableHairstyles.Count;
        for (int i = 0; i < Shown && Main.hairStart + i < count; i++)
        {
            int style = Main.Hairstyles.AvailableHairstyles[Main.hairStart + i];
            Rectangle cell = HudPoints.Units(_styles[i].Visual);
            Main.instance.LoadHair(style);
            Texture2D hair = TextureAssets.PlayerHair[style].Value;
            var head = new Vector2(cell.Center.X - hair.Width / 2, cell.Y + 4);
            var frame = new Rectangle(0, 0, hair.Width, 56);
            batch.Draw(TextureAssets.Players[0, 0].Value, head, frame, player.skinColor);
            batch.Draw(TextureAssets.Players[0, 1].Value, head, frame, Color.White);
            batch.Draw(TextureAssets.Players[0, 2].Value, head, frame, player.eyeColor);
            batch.Draw(hair, head + player.GetHairDrawOffset(style, hatHair: false) * player.Directions, frame, Main.selColor);
        }
    }
}
