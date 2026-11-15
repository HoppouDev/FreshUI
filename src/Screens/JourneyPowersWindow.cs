using System.Linq.Expressions;
using System.Reflection;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.States;

namespace FreshUI.Screens;

/// <summary>
/// The journey powers (the game's <see cref="UICreativePowersMenu"/> on <see cref="CreativeUI"/>'s own user
/// interface, beside the inventory of a journey character) in FreshUI's components, by <see cref="GameUiMirror"/>:
/// the power strips' buttons, their sliders, and the items window (duplication and research). The game's menu keeps
/// its state and handlers and runs its update from here (the buttons' tooltips among it); its interface no longer
/// draws or updates it (<see cref="GumRoutes.State{T}"/>). On the gamepad the menu's own points and links are the
/// game's (placed from here, as its draw did), and the mirror follows the game's cursor along them (A clicks what's
/// under it). The menu's toggle button stays the game's.
/// </summary>
internal sealed class JourneyPowersWindow : ContainerRuntime
{
    static JourneyPowersWindow() => GumRoutes.State<UICreativePowersMenu>();

    private static readonly Func<CreativeUI, UICreativePowersMenu?> Menu = Compile();

    // The menu's gamepad points (its Draw placed them): its own links along its strips, sliders and item grid.
    private static readonly Action<UICreativePowersMenu> SetupGamepadPoints = (Action<UICreativePowersMenu>)Delegate.CreateDelegate(typeof(Action<UICreativePowersMenu>),
        typeof(UICreativePowersMenu).GetMethod("SetupGamepadPoints", BindingFlags.NonPublic | BindingFlags.Instance)!);

    private readonly GameUiMirror _mirror = new() { FollowsGameCursor = true };

    public JourneyPowersWindow()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Visible = false;
        Children.Add(_mirror);
    }

    /// <summary>Whether the game would show the menu: open beside the inventory of a journey character.</summary>
    public static bool Shows => Main.playerInventory && !Main.inFancyUI && Main.LocalPlayer.difficulty == 3 &&
        Main.CreativeMenu.Enabled && !Main.CreativeMenu.Blocked;

    /// <summary>Each frame, at draw time (the game's cursor in UI units, as where the game drew the menu).</summary>
    public void Present()
    {
        bool shown = Shows;
        if (Visible != shown)
            Visible = shown;
        UICreativePowersMenu? menu = shown ? Menu(Main.CreativeMenu) : null;
        _mirror.Source = menu;
        if (menu == null)
            return;
        _mirror.Update();
        SetupGamepadPoints(menu);
    }

    private static Func<CreativeUI, UICreativePowersMenu?> Compile()
    {
        ParameterExpression ui = Expression.Parameter(typeof(CreativeUI));
        FieldInfo state = typeof(CreativeUI).GetField("_uiState", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return Expression.Lambda<Func<CreativeUI, UICreativePowersMenu?>>(Expression.Field(ui, state), ui).Compile();
    }
}
