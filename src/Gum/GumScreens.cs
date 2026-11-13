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

    /// <summary>Adds a screen, shown at first if <paramref name="initiallyVisible" />.</summary>
    public static GumScreen Register(string id, Func<GraphicalUiElement> create, bool initiallyVisible = true)
    {
        if (Registered.Any(screen => screen.Id == id))
        {
            throw new ArgumentException($"A Gum screen {id} is already registered.", nameof(id));
        }
        var registered = new GumScreen(id, create, initiallyVisible);
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

/// <summary>A registered Gum screen (see <see cref="GumScreens" />).</summary>
public sealed class GumScreen
{
    private readonly Func<GraphicalUiElement> _create;

    private bool _visible;

    internal GumScreen(string id, Func<GraphicalUiElement> create, bool visible)
    {
        Id = id;
        _create = create;
        _visible = visible;
    }

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
            if (Root != null)
            {
                Root.Visible = value;
            }
        }
    }

    /// <summary>Builds the screen again (after a hot reload), keeping whether it shows.</summary>
    internal void Rebuild()
    {
        if (Root == null)
        {
            return;
        }
        Root.RemoveFromRoot();
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
        Root.Visible = _visible;
        Root.AddToRoot();
    }
}
