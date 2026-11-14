using Gum.Wireframe;

namespace FreshUI.GumUi;

/// <summary>
/// Gum screens shown in game. Register each from the mod initializer with an ID and a factory that
/// builds its root element; <see cref="GumHost" /> builds it on the game thread once Gum is ready,
/// adds it to Gum's root and draws it over the HUD while in a world (in Aether's retained mod UI layer,
/// suspended in menus, settings and the full-screen map, like the game's own UI). Screens persist
/// between worlds. Their components appear in remote UI automation as the <c>gum</c> tree
/// (<see cref="GumUiTree" />), with the screen's ID as each component's <c>screen</c>.
/// </summary>
public static class GumScreens
{
    private static readonly List<GumScreen> Registered = [];

    internal static IReadOnlyList<GumScreen> All => Registered;

    /// <summary>
    /// Adds a screen, shown at first if <paramref name="initiallyVisible" />, on <paramref name="layer" />:
    /// the HUD (in a world, where the game draws its HUD) or the overlay (menus: over everything but
    /// mouse text and the cursor, on the title menu and in game, the pause menu and settings included).
    /// </summary>
    public static GumScreen Register(string id, Func<GraphicalUiElement> create, bool initiallyVisible = true, GumLayer layer = GumLayer.Hud,
        Func<bool>? showWhen = null, Func<Gum.Forms.Controls.FrameworkElement?>? defaultFocus = null, Func<string>? hints = null,
        Action<int>? pageMove = null)
    {
        if (Registered.Any(screen => screen.Id == id))
        {
            throw new ArgumentException($"A Gum screen {id} is already registered.", nameof(id));
        }
        var registered = new GumScreen(id, create, initiallyVisible, layer)
        {
            ShowWhen = showWhen,
            DefaultFocus = defaultFocus,
            Hints = hints,
            PageMove = pageMove,
        };
        Registered.Add(registered);
        return registered;
    }

    /// <summary>The ID of the screen <paramref name="element" /> belongs to, if any.</summary>
    internal static string? ScreenOf(GraphicalUiElement element)
    {
        for (GraphicalUiElement? at = element; at != null; at = at.Parent as GraphicalUiElement)
        {
            foreach (GumScreen screen in Registered)
            {
                if (screen.Root == at)
                {
                    return screen.Id;
                }
            }
        }
        return null;
    }
}

/// <summary>A screen's root with per-update work (laying itself out, reading the game's state) while it shows.</summary>
public interface IGumScreenUpdate
{
    void UpdateScreen();
}

/// <summary>Where a Gum screen draws (see <see cref="GumScreens.Register" />).</summary>
public enum GumLayer
{
    Hud,
    Overlay,
}

/// <summary>A registered Gum screen (see <see cref="GumScreens" />).</summary>
public sealed class GumScreen
{
    private readonly Func<GraphicalUiElement> _create;

    private bool _visible;

    internal GumScreen(string id, Func<GraphicalUiElement> create, bool visible, GumLayer layer)
    {
        Id = id;
        _create = create;
        _visible = visible;
        Layer = layer;
    }

    public GumLayer Layer { get; }

    /// <summary>When set, the screen shows only while this holds too (checked every update): a menu that follows the game's state.</summary>
    public Func<bool>? ShowWhen { get; init; }

    /// <summary>
    /// The control the gamepad's focus starts on; a screen with one takes the gamepad while it shows on
    /// the overlay (see <see cref="GumGamepad"/>).
    /// </summary>
    public Func<Gum.Forms.Controls.FrameworkElement?>? DefaultFocus { get; init; }

    /// <summary>The screen's own instruction bar entries (as <c>PlayerInput.BuildCommand</c> makes them), after the focused control's.</summary>
    public Func<string>? Hints { get; init; }

    /// <summary>The gamepad's previous or next page (−1, 1: the gamepad UI's HotbarMinus and HotbarPlus, LT and RT), as between a window's tabs.</summary>
    public Action<int>? PageMove { get; init; }

    /// <summary>Whether the screen shows now.</summary>
    public bool IsShown => Root is { Visible: true };

    public string Id { get; }

    /// <summary>The screen's root element, once built (on the first frame in a world).</summary>
    public GraphicalUiElement? Root { get; private set; }

    /// <summary>Whether the screen shows; set on the game thread.</summary>
    public bool IsVisible
    {
        get => _visible;
        set
        {
            _visible = value;
            Sync();
        }
    }

    /// <summary>Builds the screen again (after a hot reload), keeping whether it shows.</summary>
    internal void Rebuild()
    {
        if (Root == null)
        {
            return;
        }
        if (Layer == GumLayer.Overlay)
        {
            GumHost.OverlayRoot.Children.Remove(Root);
        }
        else
        {
            Root.RemoveFromRoot();
        }
        Root = null;
        Build();
    }

    /// <summary>Builds the screen and adds it to Gum's root, the first time.</summary>
    internal void Build()
    {
        if (Root != null)
        {
            return;
        }
        Root = _create();
        if (Layer == GumLayer.Overlay)
        {
            GumHost.OverlayRoot.Children.Add(Root);
        }
        else
        {
            Root.AddToRoot();
        }
        Sync();
    }

    /// <summary>Shows the root while the screen is visible, a HUD screen only in a world (its layer isn't drawn on the title menu, and its elements would take the mouse there).</summary>
    internal void Sync()
    {
        bool shown = _visible && (Layer != GumLayer.Hud || !Terraria.Main.gameMenu) && (ShowWhen?.Invoke() ?? true);
        if (Root != null && Root.Visible != shown)
        {
            Root.Visible = shown;
        }
    }
}
