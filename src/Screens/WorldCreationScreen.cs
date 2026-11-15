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
/// The world creation (the game's <see cref="UIWorldCreation"/>: name and seed, size, difficulty and evil, the
/// preview and the option's description, Back and Create) in FreshUI's components, by <see cref="GameUiMirror"/>:
/// the game's screen keeps its options, handlers and layout; Gum draws it and takes the input. The secret seeds'
/// ring and particles are the game's, drawn over it. B goes back as the game's back button.
/// </summary>
internal sealed class WorldCreationScreen : ContainerRuntime, IGumScreenUpdate
{
    public static readonly Func<bool> Shows = GumRoutes.State<UIWorldCreation>();

    public static WorldCreationScreen? Instance { get; private set; }

    private readonly GameUiMirror _mirror = new();

    public WorldCreationScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(_mirror);
        Children.Add(new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, _) => (_mirror.Source as UIWorldCreation)?.DrawSeedSystems(batch),
        });
        Instance = this;
    }

    public Gum.Forms.Controls.FrameworkElement? First => _mirror.First;

    public void UpdateScreen()
    {
        _mirror.Source = Main.MenuUI.CurrentState as UIWorldCreation;
        _mirror.Update();
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock && _mirror.Source is IHaveBackButtonCommand back)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            back.HandleBackButtonUsage();
        }
    }
}
