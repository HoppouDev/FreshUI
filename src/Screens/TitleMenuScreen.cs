using System.Linq.Expressions;
using System.Reflection;
using Aether.UI.Menu;
using FreshUI.Components;
using FreshUI.GumUi;
using FreshUI.Legacy;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The title menu's own screens (Aether's <see cref="UIMenuScreen"/>s: the main menu, multiplayer, joining and
/// hosting, the delete confirmations, connection status and errors, language…), in FreshUI's style: each item a
/// <see cref="FreshPanelButton"/> (a label: Gum text) centred where Aether's screen puts it, its text kept up to
/// date (a text field's typing and caret are the screen's), disabled as the item is, a click (or A) doing what the
/// item does. Aether's screen keeps its per-frame work and Back (<c>MenuScreens</c>); it's only no longer drawn or
/// updated (<see cref="GumRoutes.StateKind"/>).
/// </summary>
internal sealed class TitleMenuScreen : ContainerRuntime, IGumScreenUpdate
{
    private const float ButtonWidth = 200, ButtonHeight = 25;

    private static readonly Func<UserInterface?> MenuUi = Expression.Lambda<Func<UserInterface?>>(
        Expression.Field(null, typeof(MenuScreens).GetField("_ui", BindingFlags.NonPublic | BindingFlags.Static)!)).Compile();
    private static readonly FieldInfo ItemText = typeof(UIMenuItem).GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ItemClick = typeof(UIMenuItem).GetField("_onClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    static TitleMenuScreen() => GumRoutes.StateKind(state => state is UIMenuScreen);

    /// <summary>The menu screen Aether shows now, if it's one of its item lists.</summary>
    private static UIMenuScreen? Current => Main.gameMenu && MenuUi()?.CurrentState is UIMenuScreen screen && screen is not EmptyMenuScreen and not MenuSettingsScreen ? screen : null;

    public static bool Shows => Current != null;

    public static TitleMenuScreen? Instance { get; private set; }

    private readonly InteractiveGue _zone = GumGamepad.SpatialZone();
    private readonly List<(UIMenuItem Item, Func<string> Text, GraphicalUiElement Visual, FreshPanelButton? Button, TextRuntime? Label)> _items = [];
    private UIMenuScreen? _shown;
    private int _shownCount;
    private float _buttonHeight = ButtonHeight;

    public TitleMenuScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(_zone);
        Instance = this;
    }

    /// <summary>The first item that can be chosen (the gamepad's focus starts there).</summary>
    public FreshPanelButton? First => _items.FirstOrDefault(i => i.Button is { IsEnabled: true }).Button;

    public void UpdateScreen()
    {
        UIMenuScreen? screen = Current;
        if (screen == null)
            return;
        int count = screen.Children.Count();
        if (screen != _shown || count != _shownCount)
            Build(screen, count);
        foreach ((UIMenuItem item, Func<string> text, GraphicalUiElement visual, FreshPanelButton? button, TextRuntime? label) in _items)
        {
            string value = text() ?? "";
            if (button != null)
            {
                if (button.Text != value)
                    button.Text = value;
                bool enabled = item.IsInteractive;
                if (button.IsEnabled != enabled)
                    button.IsEnabled = enabled;
            }
            else if (label!.Text != value)
                label.Text = value;
            // Centred where Aether's screen puts the item (its top in UI units, two a UI pixel; 50 units a line).
            float height = button != null ? _buttonHeight : 0;
            float y = MathF.Round(item.Top.Pixels / 2 + 12.5f * item.TextScale - height / 2);
            if (visual.Y != y)
                visual.Y = y;
        }
    }

    private void Build(UIMenuScreen screen, int count)
    {
        _shown = screen;
        _shownCount = count;
        _zone.Children.Clear();
        _items.Clear();
        // Buttons as tall as fits between the screen's items, a gap between them.
        float spacing = float.MaxValue, last = float.NaN;
        foreach (UIMenuItem item in screen.Children.OfType<UIMenuItem>())
        {
            if (!float.IsNaN(last) && item.Top.Pixels > last)
                spacing = MathF.Min(spacing, (item.Top.Pixels - last) / 2);
            last = item.Top.Pixels;
        }
        _buttonHeight = Math.Clamp(MathF.Floor(spacing) - 3, 16, ButtonHeight);
        foreach (UIMenuItem item in screen.Children.OfType<UIMenuItem>())
        {
            var text = (Func<string>)ItemText.GetValue(item)!;
            var onClick = (Action?)ItemClick.GetValue(item);
            if (item.IsLabel || onClick == null)
            {
                var label = new TextRuntime
                {
                    XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
                    XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center,
                    WidthUnits = DimensionUnitType.RelativeToChildren,
                    HeightUnits = DimensionUnitType.RelativeToChildren,
                    Width = 0,
                    Height = 0,
                    FontSize = 12 * item.TextScale,
                    Color = FreshStyle.MutedText,
                    HasEvents = false,
                };
                GumText.SetOutline(label, 0.75f, Color.Black);
                _zone.Children.Add(label);
                _items.Add((item, text, label, null, label));
                continue;
            }
            var button = new FreshPanelButton { FontSize = 12 * MathF.Min(1, item.TextScale) };
            button.Visual.WidthUnits = button.Visual.HeightUnits = DimensionUnitType.Absolute;
            button.Visual.Width = ButtonWidth;
            button.Visual.Height = _buttonHeight;
            button.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            button.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            button.Click += (_, _) =>
            {
                if (item.IsInteractive)
                    MenuPreview.Act(text() ?? "", onClick);
            };
            _zone.Children.Add(button.Visual);
            _items.Add((item, text, button.Visual, button, null));
        }
    }
}
