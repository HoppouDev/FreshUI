using FreshUI.Components;
using FreshUI.Screens;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI.Gamepad;

namespace FreshUI.GumUi;

/// <summary>
/// FreshUI's widget gallery: launched with <see cref="Parameter"/> (<c>./aether gallery</c>), the client
/// opens on a gallery of FreshUI's Gum components in place of the title menu, to prototype them with
/// .NET hot reload and drive them over Aether's MCP server. Gum runs on the title screen then
/// (<see cref="GalleryMixin"/>), drawn under the cursor, and takes the gamepad: using it focuses the
/// first control (the mouse clears the focus again). The game's instruction bar shows the focused
/// control's hints, from a gamepad page of the gallery's own, whose one point (the game's cursor) follows
/// the focus.
/// </summary>
public static class GumGallery
{
    public const string Parameter = "-freshuigallery";

    /// <summary>Whether the client was launched for the gallery (from the command line, read before Terraria's own parsing).</summary>
    public static bool Enabled { get; } = Environment.GetCommandLineArgs().Contains(Parameter);

    /// <summary>On the title menu in a gallery launch, once the game has loaded (its splash screen is over).</summary>
    public static bool IsOpen => Enabled && Main.gameMenu && !Main.showSplash && Main.menuMode is 0 or 888;

    public static void Update(GameTime time)
    {
        // The title menu shows nothing under the gallery (an empty UI state), so its buttons take no clicks.
        if (Main.menuMode == 0)
            Main.menuMode = 888;
        if (Main.menuMode == 888 && Main.MenuUI.CurrentState != null)
            Main.MenuUI.SetState(null);
        GumHost.Update(time);
        // After Gum is set up (the first update): before, its gamepads aren't made yet.
        GumHost.GamepadEnabled = true;
        // As in most games' menus: the focus shows while the gamepad is in use.
        object? focused = InteractiveGue.CurrentInputReceiver;
        if (PlayerInput.UsingGamepad && focused == null)
            GalleryScreen.FocusFirst();
        else if (!PlayerInput.UsingGamepad && focused is FrameworkElement control)
            control.IsFocused = false;
        FollowFocus();
    }

    // The gallery's gamepad page: one point, at the focused control, its hints the instruction bar's.
    // Above the game's IDs (to 30000) and GamepadMenu's (31000+).
    private const int PageId = 31100;
    private static UILinkPage? _page;

    private static void FollowFocus()
    {
        if (_page == null)
        {
            _page = new UILinkPage();
            _page.LinkMap.Add(PageId, new UILinkPoint(PageId, enabled: true, -3, -4, -1, -2));
            _page.IsValidEvent += () => IsOpen;
            _page.CanEnterEvent += () => IsOpen;
            _page.OnSpecialInteracts += Hints;
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
        ButtonBase or FreshIconButton => Hint(Lang.misc[53].Value, "A"),
        _ => "",
    };

    private static string Hint(string text, params string[] buttons) => PlayerInput.BuildCommand(text, new List<string>(buttons));
}
