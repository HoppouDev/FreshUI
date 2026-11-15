using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI.Gamepad;
using Terraria.WorldBuilding;

namespace FreshUI.Screens;

/// <summary>
/// A world being generated (the game's <see cref="UIWorldLoad"/>, its update still the game's: the inventory key
/// cancels), in the game's ornate style, built of Gum sprites: the step's message (Death Text, with the secret
/// seeds' jokes, as the game's) over the game's frame for the world's evil, the whole generation's progress in the
/// evil's colour and the step's in gold, and the game's tips.
/// </summary>
internal sealed class WorldLoadScreen : ContainerRuntime, IGumScreenUpdate
{
    public static readonly Func<bool> Shows = GumRoutes.State<UIWorldLoad>();
    // The game's bar, in UI pixels (its units halved).
    private const float BarWidth = 306;

    private readonly TextRuntime _message = new()
    {
        XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
        XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center,
        WidthUnits = DimensionUnitType.RelativeToChildren,
        HeightUnits = DimensionUnitType.RelativeToChildren,
        Width = 0,
        Height = 0,
        FontSize = GumText.DeathTextSize,
        HasEvents = false,
    };
    private readonly ContainerRuntime _bar = new() { Width = BarWidth, Height = 35, HasEvents = false };
    private readonly Fill _overall, _current;
    private readonly SpriteRuntime _frame;
    private readonly Texture2D _corrupt, _crimson, _random;

    public WorldLoadScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        GumText.SetOutline(_message, 0.6f, Color.Black);
        GumText.SetDropShadow(_message, 2);
        Children.Add(_message);
        _overall = new Fill(_bar, 10, 20, 285, 8, new Color(48, 48, 48));
        _current = new Fill(_bar, 25, 30, 254, 4, new Color(33, 33, 33));
        _corrupt = GumTextures.Load("Images/UI/WorldGen/Outer_Corrupt");
        _crimson = GumTextures.Load("Images/UI/WorldGen/Outer_Crimson");
        _random = GumTextures.Load("Images/UI/WorldGen/Outer_Random");
        _frame = Sprite(_corrupt, -4, 0);
        _bar.Children.Add(_frame);
        _bar.Children.Add(Sprite(GumTextures.Load("Images/UI/WorldGen/Outer_Lower"), 18, 30));
        Children.Add(_bar);
        Children.Add(new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (_, _) =>
            {
                if (WorldGenerator.CurrentGenerationProgress == null)
                    return;
                Main.gameTips.Update();
                Main.gameTips.Draw();
            },
        });
    }

    // The game's art at its size (2×2 texels a UI pixel).
    private static SpriteRuntime Sprite(Texture2D texture, float x, float y) => new()
    {
        Texture = texture,
        X = x,
        Y = y,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        Width = texture.Width / 2f,
        Height = texture.Height / 2f,
    };

    /// <summary>One of the game's bars: filled up to the progress, a darker pixel at its end, the rest empty.</summary>
    private sealed class Fill
    {
        private readonly ColoredRectangleRuntime _filled = new(), _end = new() { Width = 1 }, _empty = new();
        private readonly float _width;

        public Fill(ContainerRuntime parent, float x, float y, float width, float height, Color empty)
        {
            _width = width;
            foreach (ColoredRectangleRuntime part in new[] { _filled, _empty, _end })
            {
                (part.X, part.Y, part.Height) = (x, y, height);
                part.WidthUnits = part.HeightUnits = DimensionUnitType.Absolute;
                parent.Children.Add(part);
            }
            _empty.Color = empty;
        }

        public void Set(float progress, Color color)
        {
            float done = MathF.Floor(Math.Clamp(progress, 0, 1) * _width);
            if (_filled.Width != done)
            {
                _filled.Width = done;
                _empty.X = _filled.X + done;
                _empty.Width = _width - done;
                _end.X = _filled.X + done - 1;
            }
            _filled.Color = color;
            _end.Color = Color.Lerp(color, Color.Black, 0.5f);
        }
    }

    public void UpdateScreen()
    {
        if (Main.MenuUI.CurrentState is UIWorldLoad load)
            load.Update(Main.gameTimeCache);
        GenerationProgress? progress = WorldGenerator.CurrentGenerationProgress;
        bool shown = progress != null;
        if (_bar.Visible != shown)
            _bar.Visible = _message.Visible = shown;
        if (progress == null)
            return;
        // As the game's: lower on taller screens.
        float top = MathF.Round(MathHelper.Lerp(270f, 370f, Utils.GetLerpValue(600f, 700f, Main.screenHeight, clamped: true)) / 2);
        float x = MathF.Floor((GraphicalUiElement.CanvasWidth - BarWidth) / 2);
        if (_bar.X != x || _bar.Y != top)
            (_bar.X, _bar.Y, _message.Y) = (x, top, top - 35);
        // The world's evil, as the game's (a drunk world's flickering).
        bool crimson = WorldGen.crimson, random = WorldGen.generatingRandomEvil;
        if (WorldGen.drunkWorldGen && Main.rand.Next(2) == 0)
            (crimson, random) = (Main.rand.Next(2) == 0, Main.rand.Next(4) == 0);
        Texture2D frame = random ? _random : crimson ? _crimson : _corrupt;
        if (_frame.Texture != frame)
            _frame.Texture = frame;
        _overall.Set((float)progress.TotalProgress, new Color { PackedValue = random ? 4292696893u : crimson ? 4286836223u : 4283888223u });
        _current.Set((float)progress.Value, new Color { PackedValue = 4290947159u });
        Color textColor = WorldGen.tenthAnniversaryWorldGen && !Main.zenithWorld ? Color.HotPink : Color.White;
        if (_message.Color != textColor)
            _message.Color = textColor;
        string text = Message(progress);
        if (_message.Text != text)
            _message.Text = text;
        // The gamepad's cursor circles the middle of the screen, as the game's.
        Vector2 squiggle = new Vector2(MathF.Cos(Main.GlobalTimeWrappedHourly * MathF.PI * 2), MathF.Sin(Main.GlobalTimeWrappedHourly * MathF.PI * 4)) * new Vector2(30, 15) + Vector2.UnitY * 20;
        UILinkPointNavigator.Points[3000].Unlink();
        UILinkPointNavigator.SetPosition(3000, new Vector2(Main.screenWidth, Main.screenHeight) / 2f + squiggle);
    }

    // The step's message, as the game's (cancelling, and the secret seeds' jokes).
    private static string Message(GenerationProgress progress)
    {
        string text = WorldGenerator.CurrentController?.QueuedAbort == true ? Language.GetTextValue("UI.Canceling") : progress.Message ?? "";
        if (WorldGen.drunkWorldGenText && !WorldGen.placingTraps && !WorldGen.getGoodWorldGen)
        {
            text = Main.rand.Next(999999999).ToString();
            for (int i = 0; i < 3; i++)
            {
                if (Main.rand.Next(2) == 0)
                    text += Main.rand.Next(999999999);
            }
        }
        if (WorldGen.notTheBees && !Main.zenithWorld)
            text = Language.GetTextValue("UI.WorldGenEasterEgg_GeneratingBees");
        if (WorldGen.getGoodWorldGen && (!WorldGen.noTrapsWorldGen || !WorldGen.placingTraps))
            text = new string(text.Reverse().ToArray());
        return text;
    }
}
