using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;

namespace FreshUI.Screens;

/// <summary>
/// An invasion's or event's progress at the bottom right (Aether's <see cref="UIInvasionProgress"/>, shown for a
/// while whenever it changes and fading in and out, as the game's): a <see cref="FreshPanel"/> in the event's
/// colour with its icon and name, and under it one with the wave (or "cleared") text over a
/// <see cref="FreshProgressBar"/>.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> InvasionAsked = GumRoutes.GameScreen<UIInvasionProgress>();
    private static Color ProgressPanelColor => new Color(63, 65, 151, 255) * 0.785f;

    private ContainerRuntime? _invasion;
    private FreshPanel? _invasionTitle, _invasionPanel;
    private SpriteRuntime? _invasionIcon;
    private TextRuntime? _invasionName, _invasionText;
    private FreshProgressBar? _invasionBar;
    private (int Progress, int Max, int Wave, GameCulture? Culture) _invasionShown = (-2, 0, 0, null);

    private void PresentInvasion()
    {
        bool shown = IsReady && InvasionAsked();
        if (_invasion == null)
        {
            if (!shown)
                return;
            CreateInvasion();
        }
        if (_invasion!.Visible != shown)
            _invasion.Visible = shown;
        if (!shown)
            return;
        float alpha = Main.invasionProgressAlpha;
        float right = GraphicalUiElement.CanvasWidth, bottom = GraphicalUiElement.CanvasHeight;
        // The progress: the wave and its progress, or how far to clearing the invasion.
        bool waves = Main.invasionProgressWave > 0;
        bool hasProgress = waves || Main.invasionProgressMax != 0;
        _invasionPanel!.Visible = hasProgress;
        if (hasProgress)
        {
            if (_invasionShown != (Main.invasionProgress, Main.invasionProgressMax, Main.invasionProgressWave, Language.ActiveCulture))
            {
                _invasionShown = (Main.invasionProgress, Main.invasionProgressMax, Main.invasionProgressWave, Language.ActiveCulture);
                string? progress = Main.invasionProgressMax != 0 ? (int)(Main.invasionProgress * 100f / Main.invasionProgressMax) + "%" : null;
                _invasionText!.Text = waves
                    ? Language.GetTextValue("Game.WaveMessage", Main.invasionProgressWave, progress ?? Language.GetTextValue("Game.InvasionPoints", Main.invasionProgress))
                    : Language.GetTextValue("Game.WaveCleared", progress);
            }
            float fill = Main.invasionProgressMax == 0 ? 1 : MathHelper.Clamp((float)Main.invasionProgress / Main.invasionProgressMax, 0f, 1f);
            _invasionBar!.Value = fill;
            _invasionBar.Opacity = alpha;
            Tint(_invasionPanel, ProgressPanelColor, alpha);
            SetColor(_invasionText!, Color.White * alpha);
            Move(_invasionPanel, right - 110, bottom - 32);
        }
        // The event's icon and name above it, on a panel in its colour.
        UIInvasionTitle.Describe(Main.invasionProgressIcon, out Texture2D icon, out string name, out Color color);
        if (_invasionName!.Text != name)
            _invasionName.Text = name;
        if (_invasionIcon!.Texture != icon)
            (_invasionIcon.Texture, _invasionIcon.Width, _invasionIcon.Height) = (icon, MathF.Round(icon.Width * 0.4f), MathF.Round(icon.Height * 0.4f));
        if (_invasionIcon.Color != Color.White * alpha)
            _invasionIcon.Color = Color.White * alpha;
        SetColor(_invasionName, Color.White * alpha);
        float width = MathF.Ceiling(_invasionName.GetAbsoluteWidth()) + _invasionIcon.Width + 12, height = MathF.Max(_invasionIcon.Height, 12) + 6;
        if (_invasionTitle!.Width != width || _invasionTitle.Height != height)
        {
            (_invasionTitle.Width, _invasionTitle.Height) = (width, height);
            _invasionIcon.Y = MathF.Round((height - _invasionIcon.Height) / 2);
            _invasionName.X = _invasionIcon.X + _invasionIcon.Width + 4;
            _invasionName.Y = MathF.Round((height - 10) / 2);
        }
        Tint(_invasionTitle, color, alpha);
        Move(_invasionTitle, right - 10 - width, bottom - 32 - (hasProgress ? 4 : 0) - height);
    }

    private static void SetColor(TextRuntime text, Color color)
    {
        if (text.Color != color)
            text.Color = color;
    }

    private static void Tint(FreshPanel panel, Color color, float alpha)
    {
        Color background = color * alpha, border = Color.Black * (color.A / 255f * alpha);
        if (panel.BackgroundColor != background)
            panel.BackgroundColor = background;
        if (panel.BorderColor != border)
            panel.BorderColor = border;
    }

    private static void Move(GraphicalUiElement element, float x, float y)
    {
        (x, y) = (MathF.Round(x), MathF.Round(y));
        if (element.X != x || element.Y != y)
            (element.X, element.Y) = (x, y);
    }

    private void CreateInvasion()
    {
        _invasion = new ContainerRuntime { WidthUnits = DimensionUnitType.RelativeToParent, HeightUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = 0, HasEvents = false };
        Children.Insert(0, _invasion);
        _invasionPanel = new FreshPanel { Width = 100, Height = 23 };
        _invasionText = Label(9);
        _invasionText.WidthUnits = DimensionUnitType.RelativeToParent;
        _invasionText.Width = 0;
        _invasionText.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        _invasionText.Y = 2;
        _invasionPanel.Children.Add(_invasionText);
        _invasionBar = new FreshProgressBar { X = 8, Y = 12, Width = 84 };
        _invasionPanel.Children.Add(_invasionBar);
        _invasion.Children.Add(_invasionPanel);
        _invasionTitle = new FreshPanel();
        _invasionIcon = new SpriteRuntime { X = 4, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
        _invasionName = Label(9.5f);
        _invasionTitle.Children.Add(_invasionIcon);
        _invasionTitle.Children.Add(_invasionName);
        _invasion.Children.Add(_invasionTitle);
    }

    private static TextRuntime Label(float size)
    {
        var text = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = size,
            HasEvents = false,
        };
        GumText.SetOutline(text, 0.5f, Color.Black);
        return text;
    }
}
