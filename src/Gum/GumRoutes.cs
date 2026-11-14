using Aether.UI.Game;
using Aether.UI.Menu;
using Gum.Wireframe;
using Terraria;
using Terraria.UI;

namespace FreshUI.GumUi;

/// <summary>
/// Gum screens in place of the game's (and Aether's): each route says what a Gum screen replaces and
/// answers whether it shows (a <see cref="GumScreens.Register"/> <c>showWhen</c>), and the game's screen is
/// switched off (<c>FreshUI.Hooks.Mixins</c>):
/// <list type="bullet">
/// <item>an Aether in-game screen (<see cref="UIGameScreen"/>, by type): <c>GameScreens.Draw</c> no longer
/// draws or updates it, and the Gum screen shows while the game asks for it (each frame it would have drawn);</item>
/// <item>a title menu mode (<c>Main.menuMode</c>): <c>MenuScreens.GetScreen</c> answers an empty screen;</item>
/// <item>a game <see cref="UIState"/> type: while one is a <see cref="UserInterface"/>'s state (the title
/// menu's <c>Main.MenuUI</c>, the in-game fancy UI's <c>Main.InGameUI</c>), that interface neither updates nor draws.</item>
/// </list>
/// </summary>
public static class GumRoutes
{
    private static readonly Dictionary<Type, (uint Asked, GumScreen? Screen)> GameScreenTypes = [];
    private static readonly HashSet<int> MenuModes = [];
    private static readonly HashSet<Type> StateTypes = [];

    /// <summary>A Gum screen in place of the Aether in-game screen of type <typeparamref name="T"/>: shown while the game asks for it.</summary>
    public static Func<bool> GameScreen<T>() where T : UIGameScreen
    {
        GameScreenTypes[typeof(T)] = (0, null);
        return () => GameScreenTypes.TryGetValue(typeof(T), out var asked) && Main.GameUpdateCount - asked.Asked <= 1;
    }

    /// <summary>Ties a registered screen to its game screen route, so it shows in the very frame the game first asks.</summary>
    public static void Bind<T>(GumScreen screen) where T : UIGameScreen => GameScreenTypes[typeof(T)] = (GameScreenTypes.GetValueOrDefault(typeof(T)).Asked, screen);

    /// <summary><c>GameScreens.Draw</c>: whether the screen is Gum's (the game's isn't drawn), noting that the game asked for it.</summary>
    public static bool AskGameScreen(UIGameScreen screen)
    {
        if (!GameScreenTypes.TryGetValue(screen.GetType(), out var route))
            return false;
        GameScreenTypes[screen.GetType()] = (Main.GameUpdateCount, route.Screen);
        // Shown at once (its visibility is otherwise synced each update): no frame without it.
        if (route.Screen is { Root: GraphicalUiElement root, IsVisible: true } && !root.Visible)
            root.Visible = true;
        return true;
    }

    /// <summary>A Gum screen in place of the title menu's screens for <paramref name="modes"/>: shown while the menu is in one.</summary>
    public static Func<bool> MenuMode(params int[] modes)
    {
        MenuModes.UnionWith(modes);
        return () => Main.gameMenu && modes.Contains(Main.menuMode);
    }

    /// <summary>Whether the title menu's screen for <paramref name="mode"/> is Gum's.</summary>
    public static bool IsMenuMode(int mode) => MenuModes.Contains(mode);

    /// <summary>A Gum screen in place of the game's <typeparamref name="T"/> UI state: shown while it's the title menu's or the fancy UI's state.</summary>
    public static Func<bool> State<T>() where T : UIState
    {
        StateTypes.Add(typeof(T));
        return () => Main.MenuUI?.CurrentState is T || Main.InGameUI?.CurrentState is T && Main.inFancyUI;
    }

    /// <summary>Whether a user interface's state is Gum's (the interface neither updates nor draws it).</summary>
    public static bool IsState(UIState? state) => state != null && StateTypes.Contains(state.GetType());
}

/// <summary>The title menu's screen for a mode a Gum screen replaces: nothing of its own.</summary>
public sealed class EmptyMenuScreen : UIMenuScreen
{
    public static readonly EmptyMenuScreen Instance = new();

    protected override void BuildItems()
    {
    }
}
