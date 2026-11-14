using FreshUI.Components;
using FreshUI.GumUi;
using FreshUI.Legacy;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;

namespace FreshUI.Screens;

/// <summary>
/// The one settings window, in game (the pause menu's Settings, over the game dimmed) and on the title
/// menu (Settings, and every settings page's mode), as the Mods window: a "Settings" title tab over two
/// panels, the tabs in the left one with the window's actions (Back; on the title menu, Language and
/// Back) at its bottom, and the open tab's name and options in the right one, scrolling when they
/// don't fit. What's on each tab is <see cref="SettingsTabs"/>'s; the open tab is kept between openings.
/// On the gamepad, the focus moves between tabs and options by position, LT/RT switch tabs, and a
/// focused slider steps with the D-pad.
/// </summary>
public sealed class SettingsScreen : ContainerRuntime, IGumScreenUpdate
{
    // In UI pixels (the legacy window's units halved).
    private const float WindowWidth = 480, GameHeight = 320, GameMargin = 24, MenuTop = 109, MenuMargin = 22;
    private const float Gap = 5, Padding = 6, LeftShare = 0.3f;
    private const float TabHeight = 20, ActionHeight = 25, HeadingHeight = 20;
    private const float RowHeight = 16, HeaderHeight = 14, RowGap = 2, Indent = 7;
    private const float RowText = 9, TabText = 10;

    /// <summary>Whether this is the in-game window (the title menu's has Language, Map Enabled, and its own Back).</summary>
    public readonly bool InGame;

    private readonly FreshWindow _window;
    private readonly FreshPanel _tabPanel;
    private readonly FreshPanel _optionPanel;
    private readonly FreshLabel _heading;
    private readonly FreshScrollViewer _list;
    private readonly List<(SettingsTab Tab, FreshPanelButton Button)> _tabs = [];
    private readonly List<FreshPanelButton> _actions = [];
    private readonly List<Action> _refresh = [];
    private readonly List<FreshSlider> _ramps = [];
    private SettingsTab _builtTab = (SettingsTab)(-1);
    private FrameworkElement? _lastFocused;

    public SettingsScreen(bool inGame)
    {
        InGame = inGame;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        // Modal in game (the game takes no clicks while it shows), over the game dimmed.
        HasEvents = inGame;
        if (inGame)
            Children.Add(FreshStyle.Backdrop(FreshStyle.Dim));
        _window = new FreshWindow(Lang.menu[14].Value);
        InteractiveGue zone = GumGamepad.SpatialZone();
        zone.Children.Add(_window);
        Children.Add(zone);

        _tabPanel = new FreshPanel();
        _window.Add(_tabPanel);
        foreach (SettingsTab tab in Enum.GetValues<SettingsTab>())
        {
            FreshPanelButton button = Button(SettingsTabs.Name(tab), TabText, () => SettingsTabs.Open(tab));
            button.AlignTextLeft(Indent);
            button.Visual.Height = TabHeight;
            _tabs.Add((tab, button));
            _tabPanel.Children.Add(button.Visual);
        }
        foreach ((Func<string> text, Action onClick) in SettingsTabs.Actions(inGame))
        {
            FreshPanelButton button = Button(text(), 12, onClick);
            button.Visual.Height = ActionHeight;
            _actions.Add(button);
            _tabPanel.Children.Add(button.Visual);
        }

        _optionPanel = new FreshPanel();
        _window.Add(_optionPanel);
        _heading = new FreshLabel { FontSize = 12.5f, OutlineWidth = 0.75f };
        _heading.Visual.X = Padding + 2;
        _heading.Visual.Y = Padding + 1;
        _optionPanel.Children.Add(_heading.Visual);
        _list = new FreshScrollViewer { WheelStep = RowHeight + RowGap };
        // The options take the focus, not the area they scroll in.
        _list.IsFocusable = false;
        _list.Visual.X = Padding;
        _list.Visual.Y = Padding + HeadingHeight;
        _list.Visual.WidthUnits = _list.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        _list.Visual.Width = -2 * Padding;
        _list.Visual.Height = -(2 * Padding + HeadingHeight);
        _list.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        _list.InnerPanel.StackSpacing = RowGap;
        _optionPanel.Children.Add(_list.Visual);
    }

    // A panel button in the window's style, its sizes in UI pixels, filling its panel's width.
    private FreshPanelButton Button(string text, float fontSize, Action onClick)
    {
        var button = new FreshPanelButton { Text = text, FontSize = fontSize };
        button.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        button.Visual.HeightUnits = DimensionUnitType.Absolute;
        button.Click += (_, _) => onClick();
        button.GotFocus += (_, _) => _lastFocused = button;
        return button;
    }

    /// <summary>The gamepad's starting point: what had the focus last, or the open tab.</summary>
    public FrameworkElement? DefaultFocus =>
        _lastFocused is { Visual.Parent: not null } last && IsInWindow(last) ? last : _tabs.Find(t => t.Tab == SettingsTabs.Tab).Button;

    private bool IsInWindow(FrameworkElement control)
    {
        for (var at = control.Visual as IRenderableIpso; at != null; at = at.Parent)
        {
            if (at == _window)
                return true;
        }
        return false;
    }

    public void UpdateScreen()
    {
        if (_builtTab != SettingsTabs.Tab)
            Build();
        foreach ((SettingsTab tab, FreshPanelButton button) in _tabs)
            button.IsChosen = tab == SettingsTabs.Tab;
        foreach (Action refresh in _refresh)
            refresh();
        if (SettingsTabs.Tab == SettingsTab.Cursor)
        {
            SettingsTabs.UpdateCursorColors();
            foreach (FreshSlider ramp in _ramps)
                ramp.UpdateRamp();
        }
        Layout();
        if (InGame)
            PauseMenu.SetArea(_window);
    }

    private void Build()
    {
        _builtTab = SettingsTabs.Tab;
        _heading.Text = SettingsTabs.Name(_builtTab);
        _list.InnerPanel.Children.Clear();
        _refresh.Clear();
        _ramps.Clear();
        SettingsTabs.Build(_builtTab, this);
        _list.VerticalScrollBarValue = 0;
    }

    private void Layout()
    {
        float canvasWidth = Gum.GumService.Default.CanvasWidth, canvasHeight = Gum.GumService.Default.CanvasHeight;
        float width = MathF.Floor(MathF.Min(WindowWidth, canvasWidth * 0.88f));
        float height, top;
        if (InGame)
        {
            height = MathF.Floor(MathF.Min(GameHeight, canvasHeight - GameMargin * 2));
            top = MathF.Floor((canvasHeight - height) / 2 + GameMargin / 4);
        }
        else
        {
            top = MenuTop;
            height = MathF.Floor(canvasHeight - MenuTop - MenuMargin);
        }
        _window.X = MathF.Floor((canvasWidth - width) / 2);
        _window.Y = top;
        _window.Width = width;
        _window.Height = height;
        _window.Layout();
        float left = MathF.Floor(width * LeftShare);
        Place(_tabPanel, 0, left, height);
        Place(_optionPanel, left + Gap, width - left - Gap, height);
        for (int i = 0; i < _tabs.Count; i++)
        {
            GraphicalUiElement tab = _tabs[i].Button.Visual;
            tab.X = Padding;
            tab.Y = Padding + i * (TabHeight + RowGap);
            tab.Width = -2 * Padding;
        }
        // The actions at the bottom of the left panel, under the tabs.
        for (int i = 0; i < _actions.Count; i++)
        {
            GraphicalUiElement action = _actions[i].Visual;
            action.X = Padding;
            action.Y = height - Padding - (_actions.Count - i) * ActionHeight - (_actions.Count - 1 - i) * Gap;
            action.Width = -2 * Padding;
        }
    }

    private static void Place(GraphicalUiElement panel, float x, float width, float height)
    {
        panel.X = x;
        panel.Width = width;
        panel.Height = height;
    }

    // --- rows, for SettingsTabs ----------------------------------------------------------------

    /// <summary>A group's name, in muted text, low in its space (close to the group it names).</summary>
    public void Header(Func<string> text)
    {
        var row = new ContainerRuntime { WidthUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = HeaderHeight };
        var label = new FreshLabel { Text = text(), FontSize = RowText, Color = FreshStyle.MutedText };
        label.Visual.X = 2;
        label.Visual.YUnits = GeneralUnitType.PixelsFromLarge;
        label.Visual.YOrigin = VerticalAlignment.Bottom;
        row.Children.Add(label.Visual);
        _list.InnerPanel.Children.Add(row);
    }

    /// <summary>A setting changed in place by clicking it (with the menus' tick), its text its state.</summary>
    public void Toggle(Func<string> text, Action change) => Button(text, () =>
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        change();
    });

    /// <summary>A row that does something else (opens another screen), making its own sound.</summary>
    public void Button(Func<string> text, Action onClick)
    {
        FreshPanelButton row = Button(text(), RowText, onClick);
        row.AlignTextLeft(Indent);
        row.Visual.Width = 0;
        row.Visual.Height = RowHeight;
        row.GotFocus += (_, _) => ScrollIntoView(row.Visual);
        // Left from an option goes to the open tab (as from the right panel to the left).
        row.SpatialNavigationLeft = _tabs.Find(t => t.Tab == _builtTab).Button;
        _refresh.Add(() =>
        {
            string now = text();
            if (row.Text != now)
                row.Text = now;
        });
        _list.InnerPanel.Children.Add(row.Visual);
    }

    /// <summary>A slider from 0 to 1: its label on the left, its bar on the right; the row lit while hovered or the bar has the focus.</summary>
    public void Slider(Func<string> label, Func<float> getValue, Action<float> setValue) => SliderRow(label, getValue, setValue, null);

    /// <summary>One of a colour's bars (hue, saturation…): a slider from 0 to 1, without a label, its track the colour at each value.</summary>
    public void ColorBar(Func<float> getValue, Action<float> setValue, Func<float, Color> ramp) => SliderRow(null, getValue, setValue, ramp);

    private void SliderRow(Func<string>? label, Func<float> getValue, Action<float> setValue, Func<float, Color>? ramp)
    {
        var row = new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = RowHeight, BackgroundColor = FreshStyle.Button };
        FreshLabel? text = null;
        if (label != null)
        {
            text = new FreshLabel { Text = label(), FontSize = RowText, OutlineWidth = 0.75f };
            text.Visual.X = Indent;
            text.Visual.YUnits = GeneralUnitType.PixelsFromMiddle;
            text.Visual.YOrigin = VerticalAlignment.Center;
            row.Children.Add(text.Visual);
        }
        var slider = new FreshSlider { Minimum = 0, Maximum = 1 };
        GraphicalUiElement bar = slider.Visual;
        bar.XUnits = label != null ? GeneralUnitType.PixelsFromLarge : GeneralUnitType.PixelsFromMiddle;
        bar.XOrigin = label != null ? HorizontalAlignment.Right : HorizontalAlignment.Center;
        bar.X = label != null ? -5 : 0;
        bar.YUnits = GeneralUnitType.PixelsFromMiddle;
        bar.YOrigin = VerticalAlignment.Center;
        // A whole pixel from the row's top: the bar is 9 pixels tall, the row 16.
        bar.Y = -0.5f;
        row.Children.Add(bar);
        if (ramp != null)
        {
            slider.Ramp = ramp;
            _ramps.Add(slider);
        }
        bool syncing = false;
        slider.Value = getValue();
        slider.ValueChanged += (_, _) =>
        {
            if (!syncing)
                setValue((float)slider.Value);
        };
        slider.GotFocus += (_, _) =>
        {
            _lastFocused = slider;
            ScrollIntoView(row);
        };
        _refresh.Add(() =>
        {
            float value = getValue();
            if (MathF.Abs((float)slider.Value - value) > 1e-4f)
            {
                syncing = true;
                slider.Value = value;
                syncing = false;
            }
            if (text != null)
            {
                string now = label!();
                if (text.Text != now)
                    text.Text = now;
            }
            bool lit = slider.IsFocused || row.HasCursorOver(FrameworkElement.MainCursor, GumHost.OverlayLayer);
            row.BackgroundColor = lit ? FreshStyle.ButtonLit : FreshStyle.Button;
            row.BorderColor = lit ? FreshStyle.Gold : Color.Black;
        });
        _list.InnerPanel.Children.Add(row);
    }

    // Scrolls the options so that a row (focused by the gamepad) is wholly in view.
    private void ScrollIntoView(GraphicalUiElement row)
    {
        float top = row.AbsoluteTop - _list.InnerPanel.AbsoluteTop;
        float view = _list.ClipContainer.AbsoluteHeight;
        double scroll = _list.VerticalScrollBarValue;
        if (top < scroll)
            _list.VerticalScrollBarValue = top;
        else if (top + row.AbsoluteHeight > scroll + view)
            _list.VerticalScrollBarValue = top + row.AbsoluteHeight - view;
    }

    // --- gamepad -------------------------------------------------------------------------------

    /// <summary>LT/RT: the previous or next tab, the focus following onto it.</summary>
    public void SwitchTab(int direction)
    {
        SettingsTab[] tabs = Enum.GetValues<SettingsTab>();
        SettingsTab next = tabs[(Array.IndexOf(tabs, SettingsTabs.Tab) + direction + tabs.Length) % tabs.Length];
        SettingsTabs.Open(next);
        _tabs.Find(t => t.Tab == next).Button.IsFocused = true;
    }

    /// <summary>The window's instruction bar entries: back or close (the inventory key, B), and switching tabs.</summary>
    public string Hints() =>
        PlayerInput.BuildCommand(!InGame || PauseMenu.SettingsFromPause ? Lang.menu[5].Value : Lang.misc[56].Value, PlayerInput.ProfileGamepadUI.KeyStatus["Inventory"])
        + PlayerInput.BuildCommand(Lang.misc[64].Value, PlayerInput.ProfileGamepadUI.KeyStatus["HotbarMinus"], PlayerInput.ProfileGamepadUI.KeyStatus["HotbarPlus"]);
}
