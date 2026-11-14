using FreshUI.Components;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI.Gamepad;

namespace FreshUI.GumUi;

/// <summary>
/// The gamepad on Gum's menus: while an overlay screen that takes it shows (one with a
/// <see cref="GumScreen.DefaultFocus"/>; the topmost, if several), Gum's controls take the gamepad
/// (<see cref="GumHost.GamepadEnabled"/>). Using the gamepad focuses the screen's default control and
/// the mouse clears the focus, as in most games' menus. The game's instruction bar shows the focused
/// control's hints, then the screen's, from a gamepad page of FreshUI's own whose one point (the game's
/// cursor) follows the focus; in a world, the HUD stays the game's own gamepad UI.
/// </summary>
public static class GumGamepad
{
    // Above the game's link IDs (to 30000).
    private const int PageId = 31100;
    private static UILinkPage? _page;
    private static GumScreen? _active;

    /// <summary>After each Gum update.</summary>
    internal static void Update()
    {
        _active = null;
        foreach (GumScreen screen in GumScreens.All)
        {
            if (screen.Layer == GumLayer.Overlay && screen.DefaultFocus != null && screen.IsShown)
                _active = screen;
        }
        GumHost.GamepadEnabled = _active != null;
        if (_active == null)
            return;
        object? focused = InteractiveGue.CurrentInputReceiver;
        if (PlayerInput.UsingGamepad && focused == null && _active.DefaultFocus!() is FrameworkElement start)
            start.IsFocused = true;
        else if (!PlayerInput.UsingGamepad && focused is FrameworkElement control)
            control.IsFocused = false;
        FollowFocus();
    }

    private static void FollowFocus()
    {
        if (_page == null)
        {
            _page = new UILinkPage();
            _page.LinkMap.Add(PageId, new UILinkPoint(PageId, enabled: true, -3, -4, -1, -2));
            _page.IsValidEvent += () => _active != null;
            _page.CanEnterEvent += () => _active != null;
            _page.OnSpecialInteracts += Hints;
            _page.OnPageMoveAttempt += direction => _active?.PageMove?.Invoke(direction);
            // On the title menu, B (the inventory key) is the menu's back, as on the game's own page there.
            _page.UpdateEvent += () =>
            {
                if (Terraria.Main.gameMenu)
                    UILinkPointNavigator.Shortcuts.BackButtonInUse = PlayerInput.Triggers.JustPressed.Inventory;
            };
            UILinkPointNavigator.RegisterPage(_page, PageId);
        }
        if (UILinkPointNavigator.CurrentPage != PageId)
            UILinkPointNavigator.ChangePage(PageId);
        if (InteractiveGue.CurrentInputReceiver is FrameworkElement { Visual: GraphicalUiElement visual })
        {
            // Gum's canvas is in UI pixels, each UiPixels.Scale screen pixels; the point is in screen pixels.
            float scale = UiPixels.Scale;
            UILinkPointNavigator.Points[PageId].Position = new Vector2(
                (visual.AbsoluteLeft + visual.AbsoluteWidth / 2) * scale, (visual.AbsoluteTop + visual.AbsoluteHeight / 2) * scale);
        }
    }

    private static string Hints() => InteractiveGue.CurrentInputReceiver switch
    {
        FreshSlider => Hint("Adjust", "DPadLeft", "DPadRight") + Hint("Adjust faster", "LeftShoulder", "RightShoulder"),
        FreshScrollViewer => Hint("Scroll", "RightStickAxis"),
        FreshToggleSwitch => Hint("Toggle", "A"),
        ButtonBase or FreshIconButton => Hint(Language.GetTextValue("LegacyMisc.53"), "A"),
        _ => "",
    } + (_active?.Hints?.Invoke() ?? "");

    /// <summary>
    /// A container the size of its parent whose controls are one spatial-navigation zone: the gamepad's
    /// focus moves to the nearest control in the direction pressed (Gum's default is tab order).
    /// </summary>
    public static InteractiveGue SpatialZone()
    {
        InteractiveGue zone = new Panel { GamepadNavigationMode = GamepadNavigationMode.Spatial }.Visual;
        zone.HasEvents = false;
        zone.WidthUnits = zone.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        zone.Width = zone.Height = 0;
        return zone;
    }

    /// <summary>An instruction bar entry: the glyphs of <paramref name="buttons"/>, then <paramref name="text"/>.</summary>
    public static string Hint(string text, params string[] buttons) => PlayerInput.BuildCommand(text, new List<string>(buttons));
}
