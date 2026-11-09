using Aether.UI.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI;

/// <summary>
/// The one settings window, in game (the pause menu's Settings) and on the title menu (Settings), in
/// the style of Aether's Mods window: a title tab over two panels, the tabs in the left one and the
/// open tab's options in the right one (scrolling when they don't fit), and the window's actions
/// (Back, and Language on the title menu) as large buttons at the bottom of the left one. Sizes are in
/// whole pixels of the art (even UI units). What's on each tab is <see cref="SettingsTabs" />.
/// </summary>
public sealed class SettingsWindow : UIElement
{
    // The Mods window's colours.
    public static readonly Color PanelColor = new Color(33, 43, 79) * 0.85f;

    public static readonly Color ButtonColor = new Color(63, 82, 151) * 0.8f;

    public static readonly Color HoverColor = new Color(73, 94, 171);

    public static readonly Color MutedText = new Color(188, 204, 240);

    private const float WindowWidth = 960f;

    // On the title menu, the window starts under the logo, as the Mods window does.
    private const float MenuTop = 218f;

    // In game, the most the window is tall, and what's kept clear above and below it.
    private const float GameHeight = 640f;

    private const float GameMargin = 48f;

    private const float Gap = 10f;

    private const float LeftShare = 0.3f;

    // The window's action buttons (as the Mods window's footer buttons).
    public const float ActionHeight = 50f;

    // Kept clear below the window on the title menu.
    private const float MenuMargin = 44f;

    // The open tab's name, over its options.
    private const float HeadingHeight = 40f;

    private const float ScrollbarWidth = 20f;

    public const float RowHeight = 32f;

    public const float HeaderHeight = 28f;

    public const float TextScale = 0.9f;

    // Text is centred on its capitals (SdfText), not on the line's height.
    private const float CapMiddle = SdfText.MouseTextCapMiddle;

    /// <summary>The open tab, kept between openings and shared by the title menu and the game.</summary>
    public static SettingsTab Tab;

    /// <summary>Where the open window is, in UI units (for IngameOptions' hover checks).</summary>
    public static Rectangle Area;

    public readonly bool InGame;

    private readonly UIPanel _tabPanel;

    private readonly UIList _tabList;

    private readonly UIPanel _optionPanel;

    private readonly UIList _list;

    private readonly UIScrollbar _scrollbar;

    private readonly UITextPanel<string> _title;

    private readonly List<SettingsButton> _tabs = new();

    private readonly List<UITextPanel<string>> _actions = new();

    private SettingsTab _builtTab = (SettingsTab)(-1);

    // The mouse wheel's position last scrolled to (see ScrollOptions).
    private int _wheel = PlayerInput.ScrollWheelValue;

    public SettingsWindow(bool inGame)
    {
        InGame = inGame;
        _tabPanel = new UIPanel
        {
            BackgroundColor = PanelColor
        };
        Append(_tabPanel);
        _tabList = new UIList
        {
            Width = StyleDimension.Fill,
            ListPadding = 4f,
            ManualSortMethod = _ => { }
        };
        _tabPanel.Append(_tabList);
        foreach (SettingsTab tab in Enum.GetValues<SettingsTab>())
        {
            SettingsButton button = new SettingsButton(SettingsTabs.Name(tab), () => Open(tab), () => Tab == tab);
            _tabs.Add(button);
            _tabList.Add(button);
        }
        _optionPanel = new UIPanel
        {
            BackgroundColor = PanelColor
        };
        Append(_optionPanel);
        _list = new UIList
        {
            Top = StyleDimension.FromPixels(HeadingHeight),
            Width = StyleDimension.FromPixelsAndPercent(-ScrollbarWidth - 4f, 1f),
            Height = StyleDimension.FromPixelsAndPercent(-HeadingHeight, 1f),
            ListPadding = 4f,
            ManualSortMethod = _ => { }
        };
        _scrollbar = new UIScrollbar
        {
            Top = StyleDimension.FromPixels(HeadingHeight + 6f),
            Height = StyleDimension.FromPixelsAndPercent(-HeadingHeight - 12f, 1f),
            HAlign = 1f,
            AutoHide = true
        };
        _list.SetScrollbar(_scrollbar);
        _optionPanel.Append(_list);
        _optionPanel.Append(_scrollbar);
        foreach ((Func<string> text, Action onClick) in SettingsTabs.Actions(inGame))
        {
            UITextPanel<string> button = ActionButton(text(), onClick);
            _actions.Add(button);
            _tabPanel.Append(button);
        }
        _title = new UITextPanel<string>(Lang.menu[14].Value, 0.8f, large: true)
        {
            HAlign = 0.5f,
            BackgroundColor = HoverColor,
            IgnoresMouseInteraction = true
        };
        _title.SetPadding(12f);
        Append(_title);
    }

    /// <summary>A large button, as the Mods window's footer buttons (lit with a gold border while hovered).</summary>
    public static UITextPanel<string> ActionButton(string text, Action onClick)
    {
        // UITextPanel draws its text at the top of its padding, so the padding centres it.
        UITextPanel<string> button = new UITextPanel<string>(text, 0.7f, large: true)
        {
            BackgroundColor = ButtonColor
        };
        button.OnMouseOver += delegate
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            Light(button, true);
        };
        button.OnMouseOut += delegate
        {
            Light(button, false);
        };
        button.OnLeftClick += delegate
        {
            onClick();
        };
        return button;
    }

    /// <summary>Opens a tab (a click on it).</summary>
    public static void Open(SettingsTab tab)
    {
        if (Tab != tab)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        Tab = tab;
    }

    /// <summary>Per-frame work, before input: switches to the open tab, and keeps what other code reads up to date.</summary>
    public void UpdateState()
    {
        if (_builtTab != Tab)
        {
            Build();
        }
        if (Tab == SettingsTab.Cursor)
        {
            SettingsTabs.UpdateCursorColors(this);
        }
        ScrollOptions();
        CalculatedStyle box = GetDimensions();
        CalculatedStyle title = _title.GetDimensions();
        Area = new Rectangle((int)box.X, (int)title.Y, (int)box.Width, (int)(box.Y + box.Height - title.Y));
        // (The hotbar ignores the mouse wheel while the window is open, so it only scrolls the options.)
        if (InGame && Area.Contains(Main.MouseScreen.ToPoint()))
        {
            Main.LocalPlayer.mouseInterface = true;
        }
        LinkGamepad();
    }

    // The game's own UIs update first and use up the wheel's turn for UIs (ScrollWheelDeltaForUI),
    // so the options scroll by the update's turn of the wheel, once (the window is drawn, and updated,
    // more often than the game updates).
    private void ScrollOptions()
    {
        int wheel = PlayerInput.ScrollWheelValue;
        if (wheel == _wheel)
        {
            return;
        }
        _wheel = wheel;
        if (PlayerInput.ScrollWheelDelta != 0 && _optionPanel.ContainsPoint(Main.MouseScreen))
        {
            _scrollbar.ViewPosition -= PlayerInput.ScrollWheelDelta;
            PlayerInput.ScrollWheelDeltaForUI = 0;
        }
    }

    private void Build()
    {
        _builtTab = Tab;
        _list.Clear();
        SettingsTabs.Build(Tab, this);
        _scrollbar.ViewPosition = 0f;
        Recalculate();
    }

    // --- rows, for SettingsTabs ---------------------------------------------------------------

    public void Header(Func<string> text) => _list.Add(new SettingsRow(text, null, header: true));

    /// <summary>A setting changed in place by clicking it (with the menus' tick).</summary>
    public void Toggle(Func<string> text, Action change) => _list.Add(new SettingsRow(text, () =>
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        change();
    }, header: false));

    /// <summary>A row that does something else (opens another screen), making its own sound.</summary>
    public void Button(Func<string> text, Action onClick) => _list.Add(new SettingsRow(text, onClick, header: false));

    public SettingsSliderRow Slider(Func<string> label, Func<float> getValue, Action<float> setValue, int gamepadFeature, Utils.ColorLerpMethod? colorMethod = null)
    {
        SettingsSliderRow row = new SettingsSliderRow(label, new UIMenuSlider(getValue, setValue, gamepadFeature, 1f, colorMethod));
        _list.Add(row);
        return row;
    }

    public IEnumerable<SettingsSliderRow> SliderRows => _list.OfType<SettingsSliderRow>();

    // --- layout --------------------------------------------------------------------------------

    private static float Even(float value) => MathF.Floor(value / 2f) * 2f;

    public override void Recalculate()
    {
        if (Parent != null)
        {
            CalculatedStyle screen = Parent.GetInnerDimensions();
            float width = Even(MathF.Min(WindowWidth, screen.Width * 0.88f));
            float top;
            float height;
            if (InGame)
            {
                height = Even(MathF.Min(GameHeight, screen.Height - GameMargin * 2f));
                top = Even((screen.Height - height) / 2f + GameMargin / 4f);
            }
            else
            {
                top = MenuTop;
                height = Even(screen.Height - MenuTop - MenuMargin);
            }
            Place(this, Even((screen.Width - width) / 2f), top, width, height);
            float left = Even(width * LeftShare);
            Place(_tabPanel, 0f, 0f, left, height);
            Place(_optionPanel, left + Gap, 0f, width - left - Gap, height);
            // The actions at the bottom of the left panel, under the tabs.
            float inner = height - _tabPanel.PaddingTop - _tabPanel.PaddingBottom;
            float actions = _actions.Count * (ActionHeight + Gap);
            _tabList.Height.Set(inner - actions, 0f);
            for (int i = 0; i < _actions.Count; i++)
            {
                _actions[i].Left.Set(0f, 0f);
                _actions[i].Width.Set(0f, 1f);
                _actions[i].Top.Set(inner - actions + Gap + i * (ActionHeight + Gap), 0f);
                _actions[i].Height.Set(ActionHeight, 0f);
            }
            _title.Top.Set(-40f, 0f);
        }
        base.Recalculate();
    }

    private static void Place(UIElement element, float left, float top, float width, float height)
    {
        element.Left.Set(left, 0f);
        element.Top.Set(top, 0f);
        element.Width.Set(width, 0f);
        element.Height.Set(height, 0f);
    }

    // --- gamepad -------------------------------------------------------------------------------

    // In game, vanilla's gamepad pages for the settings window: 30 points on the left (2900+), 30 on
    // the right (2930+). On the title menu each element adds its own (MenuScreens.AddGamepadPoint).
    private void LinkGamepad()
    {
        if (!InGame)
        {
            return;
        }
        int left = 0;
        foreach (UIElement button in _tabs.Cast<UIElement>().Concat(_actions))
        {
            if (left < 30)
            {
                UILinkPointNavigator.SetPosition(2900 + left++, button.GetDimensions().Center());
            }
        }
        UILinkPointNavigator.Shortcuts.INGAMEOPTIONS_BUTTONS_LEFT = left;
        int right = 0;
        CalculatedStyle view = _list.GetInnerDimensions();
        foreach (UIElement row in _list)
        {
            CalculatedStyle box = row.GetDimensions();
            if (right < 30 && row is not SettingsRow { IsHeader: true } && box.Y >= view.Y && box.Y + box.Height <= view.Y + view.Height)
            {
                UILinkPointNavigator.SetPosition(2930 + right++, box.Center());
            }
        }
        UILinkPointNavigator.Shortcuts.INGAMEOPTIONS_BUTTONS_RIGHT = right;
    }

    // --- drawing -------------------------------------------------------------------------------

    protected override void DrawChildren(SpriteBatch spriteBatch)
    {
        base.DrawChildren(spriteBatch);
        // The open tab's name over its options (as the Mods window names the selected mod), on its panel.
        CalculatedStyle inner = _optionPanel.GetInnerDimensions();
        DrawText(spriteBatch, SettingsTabs.Name(Tab), inner.X + 4f, inner.Y + HeadingHeight / 2f - 4f, Color.White, 1.25f);
    }

    /// <summary>
    /// Draws MouseText with a shadow, as the Mods window does, its capitals centred on
    /// <paramref name="middle" />: <paramref name="anchorX" /> 0 puts its left edge at <paramref name="x" />, 0.5 its centre.
    /// </summary>
    public static void DrawText(SpriteBatch spriteBatch, string text, float x, float middle, Color color, float scale, float anchorX = 0f)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Vector2 position = new Vector2(x - (anchorX == 0f ? 0f : font.MeasureString(text).X * scale * anchorX), middle - CapMiddle * scale);
        spriteBatch.DrawString(font, text, position + Vector2.One, Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>A panel's colours, as the Mods window's rows: lighter, with a gold border, while hovered or selected.</summary>
    public static void Light(UIPanel panel, bool lit)
    {
        panel.BackgroundColor = lit ? HoverColor : ButtonColor;
        panel.BorderColor = lit ? Colors.FancyUIFatButtonMouseOver : Color.Black;
    }
}

/// <summary>A tab in the window's left panel, lit while it's open.</summary>
public sealed class SettingsButton : UIPanel
{
    private const float ButtonHeight = 40f;

    private readonly string _text;

    private readonly Action _onClick;

    private readonly Func<bool> _isSelected;

    public SettingsButton(string text, Action onClick, Func<bool> isSelected)
    {
        _text = text;
        _onClick = onClick;
        _isSelected = isSelected;
        Width = StyleDimension.Fill;
        Height = StyleDimension.FromPixels(ButtonHeight);
        SetPadding(0f);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        _onClick();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle box = GetDimensions();
        MenuScreens.AddGamepadPoint(box.Center());
        SettingsWindow.Light(this, IsMouseHovering || _isSelected());
        base.DrawSelf(spriteBatch);
        float width = FontAssets.MouseText.Value.MeasureString(_text).X;
        float scale = MathF.Min(1f, (box.Width - 28f) / MathF.Max(1f, width));
        SettingsWindow.DrawText(spriteBatch, _text, box.X + 14f, box.Y + box.Height / 2f, Color.White, scale);
    }
}

/// <summary>One of a tab's options, a button that changes it; or a header over a group of them.</summary>
public sealed class SettingsRow : UIPanel
{
    private const float Indent = 14f;

    private readonly Func<string> _text;

    private readonly Action? _onClick;

    public readonly bool IsHeader;

    public SettingsRow(Func<string> text, Action? onClick, bool header)
    {
        _text = text;
        _onClick = onClick;
        IsHeader = header;
        Width = StyleDimension.Fill;
        Height = StyleDimension.FromPixels(header ? SettingsWindow.HeaderHeight : SettingsWindow.RowHeight);
        SetPadding(0f);
        IgnoresMouseInteraction = header;
    }

    private bool Clickable => !IsHeader && _onClick != null;

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        if (Clickable)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Clickable)
        {
            _onClick!();
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle box = GetDimensions();
        if (IsHeader)
        {
            // Low in its space, close to the group it names.
            SettingsWindow.DrawText(spriteBatch, _text(), box.X + 4f, box.Y + box.Height / 2f + 3f, SettingsWindow.MutedText, SettingsWindow.TextScale);
            return;
        }
        MenuScreens.AddGamepadPoint(box.Center());
        SettingsWindow.Light(this, IsMouseHovering && Clickable);
        base.DrawSelf(spriteBatch);
        SettingsWindow.DrawText(spriteBatch, _text(), box.X + Indent, box.Y + box.Height / 2f, Color.White, SettingsWindow.TextScale);
    }
}

/// <summary>
/// A slider: its label on the left and its bar on the right. Hovering anywhere on the row focuses the
/// bar, so the gamepad (on the row's point) adjusts it, as in vanilla's settings window; the bar
/// itself ticks when hovered.
/// </summary>
public sealed class SettingsSliderRow : UIPanel
{
    private const float Indent = 14f;

    private const float BarInset = 10f;

    private readonly Func<string> _label;

    public readonly UIMenuSlider Slider;

    /// <summary>What the row adjusts, for the tab's own bookkeeping (25/252: the cursor's or its border's colour).</summary>
    public int Tag;

    public SettingsSliderRow(Func<string> label, UIMenuSlider slider)
    {
        _label = label;
        Slider = slider;
        Width = StyleDimension.Fill;
        Height = StyleDimension.FromPixels(SettingsWindow.RowHeight);
        SetPadding(0f);
        // A bar without a label (the cursor's colour bars, under their header) is centred.
        bool labelled = label().Length > 0;
        slider.HAlign = labelled ? 1f : 0.5f;
        slider.Left.Set(labelled ? -BarInset : 0f, 0f);
        slider.Top.Set(MathF.Floor((SettingsWindow.RowHeight - slider.Height.Pixels) / 4f) * 2f, 0f);
        slider.AlsoFocused = () => IsMouseHovering;
        Append(slider);
    }

    public bool IsFocused => IsMouseHovering || Slider.IsDragging;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        SettingsWindow.Light(this, IsFocused);
        base.DrawSelf(spriteBatch);
        string label = _label();
        if (label.Length > 0)
        {
            CalculatedStyle box = GetDimensions();
            SettingsWindow.DrawText(spriteBatch, label, box.X + Indent, box.Y + box.Height / 2f, Color.White, SettingsWindow.TextScale);
        }
    }
}
