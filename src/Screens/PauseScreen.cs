using FreshUI.Components;
using FreshUI.GumUi;
using FreshUI.Legacy;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;

namespace FreshUI.Screens;

/// <summary>
/// The pause menu (<see cref="PauseMenu"/> keeps its state and actions): the game dimmed, and the menu's
/// buttons in a panel under a "Game Menu" title tab: Back to Game, Achievements | Mods, Settings |
/// Invite Friends (where invites work), Save &amp; Exit. On the overlay, while the game's settings window
/// is the pause menu; it takes the gamepad, starting at Back to Game (B, the inventory key, closes it).
/// </summary>
public sealed class PauseScreen : ContainerRuntime, IGumScreenUpdate
{
    // In UI pixels (the legacy window's units halved).
    private const float PanelWidth = 250, Padding = 8, Gap = 5, ButtonHeight = 25;

    private readonly FreshWindow _window;
    private readonly FreshPanel _panel;
    private readonly List<FreshPanelButton> _buttons = [];
    private int _builtFor = -1;

    /// <summary>The pause menu that shows, once built.</summary>
    public static PauseScreen? Current { get; private set; }

    // The last button focused since the menu opened: back from its settings, the focus is there again.
    private FreshPanelButton? _lastFocused;

    /// <summary>The gamepad's starting point: Back to Game, or the button last focused since the menu opened.</summary>
    public FrameworkElement? First => _lastFocused ?? (_buttons.Count > 0 ? _buttons[0] : null);

    public PauseScreen()
    {
        WidthUnits = DimensionUnitType.RelativeToParent;
        HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        // Modal: the game takes no clicks while it shows.
        HasEvents = true;
        Children.Add(FreshStyle.Backdrop(FreshStyle.Dim));
        _window = new FreshWindow("Game Menu") { Width = PanelWidth };
        _panel = new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, HeightUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = 0 };
        _window.Add(_panel);
        // The gamepad's focus moves to the button nearest in the direction pressed.
        Gum.Wireframe.InteractiveGue zone = GumGamepad.SpatialZone();
        zone.Children.Add(_window);
        Children.Add(zone);
        Current = this;
    }

    public void UpdateScreen()
    {
        if (_builtFor != PauseMenu.OpenCount)
        {
            Build();
        }
        // Centred, on whole pixels.
        _window.X = MathF.Floor((Gum.GumService.Default.CanvasWidth - PanelWidth) / 2);
        _window.Y = MathF.Floor((Gum.GumService.Default.CanvasHeight - _window.Height) / 2);
        _window.Layout();
        PauseMenu.SetArea(_window);
    }

    // The buttons, afresh each time the menu opens (whether invites work can change).
    private void Build()
    {
        _builtFor = PauseMenu.OpenCount;
        foreach (FreshPanelButton button in _buttons)
        {
            _panel.Children.Remove(button.Visual);
        }
        _buttons.Clear();
        _lastFocused = null;
        int rows = 0;
        Row(ref rows, ("Back to Game", () => IngameOptions.Close()));
        if (PauseMenu.HasMods)
            Row(ref rows, (Lang.menu[131].Value, PauseMenu.Achievements), (Language.GetTextValue("AetherMods.Title"), PauseMenu.Mods));
        else
            Row(ref rows, (Lang.menu[131].Value, PauseMenu.Achievements));
        if (PauseMenu.CanInvite)
            Row(ref rows, (Lang.menu[14].Value, PauseMenu.ShowSettings), (Lang.menu[147].Value, PauseMenu.Invite));
        else
            Row(ref rows, (Lang.menu[14].Value, PauseMenu.ShowSettings));
        Row(ref rows, (Lang.inter[35].Value, PauseMenu.SaveAndExit));
        _window.Height = Padding * 2 + rows * ButtonHeight + (rows - 1) * Gap;
    }

    // A row of buttons sharing the panel's width, the last one ending at its right edge.
    private void Row(ref int rows, params (string Text, Action OnClick)[] buttons)
    {
        float inner = PanelWidth - Padding * 2;
        float width = MathF.Floor((inner - Gap * (buttons.Length - 1)) / buttons.Length);
        for (int i = 0; i < buttons.Length; i++)
        {
            (string text, Action onClick) = buttons[i];
            var button = new FreshPanelButton { Text = text };
            button.Visual.WidthUnits = DimensionUnitType.Absolute;
            button.Visual.HeightUnits = DimensionUnitType.Absolute;
            button.Visual.Width = width;
            button.Visual.Height = ButtonHeight;
            button.Visual.X = Padding + (i == buttons.Length - 1 ? inner - width : i * (width + Gap));
            button.Visual.Y = Padding + rows * (ButtonHeight + Gap);
            button.Click += (_, _) => onClick();
            button.GotFocus += (_, _) => _lastFocused = button;
            _panel.Children.Add(button.Visual);
            _buttons.Add(button);
        }
        rows++;
    }

    /// <summary>The pause menu's own instruction bar entry: the inventory key (B) closes it.</summary>
    public static string Hints() => PlayerInput.BuildCommand(Lang.misc[56].Value, PlayerInput.ProfileGamepadUI.KeyStatus["Inventory"]);
}
