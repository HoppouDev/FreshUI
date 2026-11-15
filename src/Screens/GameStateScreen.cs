using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The rest of the game's screens, each by <see cref="GameUiMirror"/> with nothing of its own to add: the Workshop
/// (its hub, publishing a world or resource pack, importing worlds), resource packs (the list and a pack's
/// information), the advanced world creation and its secret seeds, the gamepad bindings, and the debug and test
/// states (world generation debug, debug commands and loadouts, reports, text wrapping). On the title menu or as a
/// fancy UI in game (over the game dimmed); B goes back as the game's back button.
/// </summary>
internal sealed class GameStateScreen : ContainerRuntime, IGumScreenUpdate
{
    private static readonly HashSet<Type> Types =
    [
        typeof(UIWorkshopHub), typeof(UIWorkshopSelectWorldToPublish), typeof(UIWorkshopSelectResourcePackToPublish),
        typeof(UIWorkshopWorldImport), typeof(WorkshopPublishInfoStateForWorld), typeof(WorkshopPublishInfoStateForResourcePack),
        typeof(UIResourcePackSelectionMenu), typeof(UIResourcePackInfoMenu),
        typeof(UIWorldCreationAdvanced), typeof(UIWorldCreationAdvancedSecretSeedsList),
        typeof(UIManageControls),
        typeof(UIWorldGenDebug), typeof(UIDebugCommandsList), typeof(UIDebugLoadoutsList), typeof(UIReportsPage), typeof(UITextWrappingTest),
    ];

    static GameStateScreen() => GumRoutes.StateKind(state => Types.Contains(state.GetType()));

    /// <summary>The game state shown, if it's one of these.</summary>
    private static UIState? Current =>
        Main.MenuUI?.CurrentState is { } menu && Types.Contains(menu.GetType()) ? menu
        : Main.inFancyUI && Main.InGameUI?.CurrentState is { } fancy && Types.Contains(fancy.GetType()) ? fancy : null;

    public static bool Shows => Current != null;

    public static GameStateScreen? Instance { get; private set; }

    private readonly RectangleRuntime _backdrop = FreshStyle.Backdrop(FreshStyle.Dim);
    private readonly GameUiMirror _mirror = new();

    public GameStateScreen()
    {
        Instance = this;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        Children.Add(_backdrop);
        Children.Add(_mirror);
    }

    public Gum.Forms.Controls.FrameworkElement? First => _mirror.First;

    public void UpdateScreen()
    {
        UIState? state = Current;
        // Modal in game (the game takes no clicks while it shows), over the game dimmed.
        bool inGame = !Main.gameMenu;
        if (_backdrop.Visible != inGame)
            _backdrop.Visible = HasEvents = inGame;
        _mirror.Source = state;
        _mirror.Update();
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock && state is IHaveBackButtonCommand back)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            back.HandleBackButtonUsage();
        }
    }
}
