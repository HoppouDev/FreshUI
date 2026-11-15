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
/// draws or updates it (<see cref="GumRoutes.State{T}"/>). The menu's toggle button stays the game's.
/// </summary>
internal sealed class JourneyPowersWindow : ContainerRuntime
{
    static JourneyPowersWindow() => GumRoutes.State<UICreativePowersMenu>();

    private static readonly Func<CreativeUI, UICreativePowersMenu?> Menu = Compile();

    private readonly GameUiMirror _mirror = new();

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
        _mirror.Source = shown ? Menu(Main.CreativeMenu) : null;
        if (shown)
            _mirror.Update();
    }

    private static Func<CreativeUI, UICreativePowersMenu?> Compile()
    {
        ParameterExpression ui = Expression.Parameter(typeof(CreativeUI));
        FieldInfo state = typeof(CreativeUI).GetField("_uiState", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return Expression.Lambda<Func<CreativeUI, UICreativePowersMenu?>>(Expression.Field(ui, state), ui).Compile();
    }
}
