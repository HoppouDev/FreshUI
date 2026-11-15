using System.Linq.Expressions;
using System.Reflection;
using Aether.UI.Menu;
using FreshUI.Components;
using FreshUI.GumUi;
using FreshUI.Legacy;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The title menu's own screens (Aether's <see cref="UIMenuScreen"/>s: the main menu, multiplayer, joining and
/// hosting, the delete confirmations, connection status and errors, language…), drawn as Aether's items: Death Text
/// outlined in black, faded until hovered or focused, when it grows and turns gold, with a soft drop shadow, centred
/// where Aether's screen puts it. Each item is a Forms <see cref="Button"/> (no backing) the size of Aether's, its
/// text kept up to date (a text field's typing and caret are the screen's), disabled as the item is, a click (or A)
/// doing what the item does; labels are Gum text. Aether's screen keeps its per-frame work and Back
/// (<c>MenuScreens</c>); it's only no longer drawn or updated (<see cref="GumRoutes.StateKind"/>).
/// </summary>
internal sealed class TitleMenuScreen : ContainerRuntime, IGumScreenUpdate
{
    private const float IdleScale = 0.8f;

    private static readonly Func<UserInterface?> MenuUi = Expression.Lambda<Func<UserInterface?>>(
        Expression.Field(null, typeof(MenuScreens).GetField("_ui", BindingFlags.NonPublic | BindingFlags.Static)!)).Compile();
    private static readonly FieldInfo ItemText = typeof(UIMenuItem).GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ItemClick = typeof(UIMenuItem).GetField("_onClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly Func<UIStatusMenu, bool> StatusShowsTips = Expression.Lambda<Func<UIStatusMenu, bool>>(
        Expression.Field(StatusParameter, typeof(UIStatusMenu).GetField("_showTips", BindingFlags.NonPublic | BindingFlags.Instance)!), StatusParameter).Compile();
    private static ParameterExpression StatusParameter => _statusParameter ??= Expression.Parameter(typeof(UIStatusMenu));
    private static ParameterExpression? _statusParameter;

    static TitleMenuScreen() => GumRoutes.StateKind(state => state is UIMenuScreen);

    /// <summary>The menu screen Aether shows now, if it's one of its item lists.</summary>
    private static UIMenuScreen? Current => Main.gameMenu && MenuUi()?.CurrentState is UIMenuScreen screen && screen is not EmptyMenuScreen and not MenuSettingsScreen ? screen : null;

    public static bool Shows => Current != null;

    public static TitleMenuScreen? Instance { get; private set; }

    private readonly InteractiveGue _zone = GumGamepad.SpatialZone();
    private readonly List<Entry> _items = [];
    private UIMenuScreen? _shown;
    private int _shownCount;

    public TitleMenuScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(_zone);
        // The loading tips, which the connecting and loading screens drew (their update still runs).
        Children.Add(new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (_, _) =>
            {
                if (Current is UIConnectingMenu || Current is UIStatusMenu status && StatusShowsTips(status))
                    Main.gameTips.Draw();
            },
        });
        Instance = this;
    }

    /// <summary>The first item that can be chosen (the gamepad's focus starts there).</summary>
    public FrameworkElement? First => _items.FirstOrDefault(i => i.Button is { IsEnabled: true })?.Button;

    public void UpdateScreen()
    {
        UIMenuScreen? screen = Current;
        if (screen == null)
            return;
        int count = screen.Children.Count();
        if (screen != _shown || count != _shownCount)
            Build(screen, count);
        // Aether's colours: the menu's tint brightened.
        byte tint = (byte)((255 + Main.tileColor.R * 2) / 3);
        int grey = (255 + tint) / 2;
        foreach (Entry entry in _items)
            entry.Update(grey);
    }

    private void Build(UIMenuScreen screen, int count)
    {
        _shown = screen;
        _shownCount = count;
        _zone.Children.Clear();
        _items.Clear();
        foreach (UIMenuItem item in screen.Children.OfType<UIMenuItem>())
        {
            var entry = new Entry(item, (Func<string>)ItemText.GetValue(item)!, (Action?)ItemClick.GetValue(item));
            _zone.Children.Add(entry.Visual);
            _items.Add(entry);
        }
    }

    /// <summary>One of Aether's items: its text, and its button unless it's a label.</summary>
    private sealed class Entry
    {
        private readonly UIMenuItem _item;
        private readonly Func<string> _text;
        private readonly TextRuntime _label;
        private float _hover = IdleScale;
        private bool _lit;

        public readonly MenuButton? Button;
        public readonly GraphicalUiElement Visual;

        public Entry(UIMenuItem item, Func<string> text, Action? onClick)
        {
            _item = item;
            _text = text;
            if (item.IsLabel || onClick == null)
            {
                _label = new TextRuntime { HasEvents = false };
                Visual = _label;
            }
            else
            {
                Button = new MenuButton(lit => _lit = lit);
                Button.Click += (_, _) =>
                {
                    if (item.IsInteractive)
                        MenuPreview.Act(text() ?? "", onClick);
                };
                Visual = Button.Visual;
                _label = ((ButtonVisual)Button.Visual).TextInstance;
            }
            _label.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            _label.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            _label.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            _label.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            _label.WidthUnits = _label.HeightUnits = DimensionUnitType.RelativeToChildren;
            _label.Width = _label.Height = 0;
            _label.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            _label.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
            GumText.SetOutline(_label, 1, Color.Black);
            GumText.SetDropShadow(_label, 2, blur: 2);
            if (Button == null)
            {
                Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            }
        }

        public void Update(int grey)
        {
            string value = _text() ?? "";
            if (_label.Text != value)
                _label.Text = value;
            float fontSize = GumText.DeathTextSize * _item.TextScale * _item.DrawScale;
            if (_label.FontSize != fontSize)
                _label.FontSize = fontSize;
            // Aether's item: as wide as its text, 50 units a line; the text centred on its first line.
            Vector2 size = FontAssets.DeathText.Value.MeasureString(value) * _item.TextScale / 2;
            float top = MathF.Round(_item.Top.Pixels / 2), middle = size.Y / 2;
            if (Button != null)
            {
                bool enabled = _item.IsInteractive;
                if (Button.IsEnabled != enabled)
                    Button.IsEnabled = enabled;
                float width = MathF.Ceiling(size.X), height = 25 * _item.TextScale;
                if (Visual.Y != top || Visual.Width != width || Visual.Height != height)
                    (Visual.Y, Visual.Width, Visual.Height) = (top, width, height);
                _label.Y = middle;
            }
            else if (Visual.Y != top + middle)
                Visual.Y = top + middle;
            // Aether's fade and growth: 0.8 to 1 while lit, faded in with it (labels whole), gold as it's lit.
            bool lit = _lit && _item.IsInteractive;
            _hover = lit ? MathF.Min(1, _hover + 0.02f) : MathF.Max(IdleScale, _hover - 0.02f);
            if (_label.FontScale != _hover)
                _label.FontScale = _hover;
            int alpha = _item.IsLabel ? 255 : (int)(255 * (_hover * 2 - 1));
            int shade = Math.Max(0, grey - (255 - alpha));
            Color color = new(shade, shade, shade, alpha);
            if (lit)
            {
                float blend = alpha / 255f;
                color = new Color((int)(shade * (1 - blend) + 255 * blend), (int)(shade * (1 - blend) + 215 * blend), (int)(shade * (1 - blend)), alpha);
            }
            if (_label.Color != color)
            {
                _label.Color = color;
                _label.DropshadowColor = GumText.DropShadowColor * (alpha / 255f);
            }
        }
    }

    /// <summary>A button with no backing: its text is all there is, lit (hovered, focused or pressed) as <paramref name="onLit"/> hears.</summary>
    private sealed class MenuButton : Button
    {
        public MenuButton(Action<bool> onLit)
        {
            var visual = (ButtonVisual)Visual;
            visual.Background.Visible = false;
            visual.FocusedIndicator.Visible = false;
            visual.WidthUnits = visual.HeightUnits = DimensionUnitType.Absolute;
            visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
            bool wasLit = false;
            foreach (StateSave state in new[] { visual.States.Enabled, visual.States.Focused, visual.States.Highlighted,
                visual.States.HighlightedFocused, visual.States.Pushed, visual.States.Disabled, visual.States.DisabledFocused })
            {
                bool lit = state != visual.States.Enabled && state != visual.States.Disabled && state != visual.States.DisabledFocused;
                state.Clear();
                state.Apply = () =>
                {
                    visual.FocusedIndicator.Visible = false;
                    if (lit && !wasLit)
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    wasLit = lit;
                    onLit(lit);
                };
            }
            UpdateState();
        }
    }
}
