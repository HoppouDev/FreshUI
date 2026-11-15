using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The bestiary (the game's <see cref="UIBestiaryTest"/>, a fancy UI from the inventory's bestiary button) in FreshUI's
/// components, by <see cref="GameUiMirror"/>, modal over the game dimmed: its search, filtering and sorting, the
/// entries' grid and its pages, and the chosen entry's portrait and information. The game's bestiary keeps its state,
/// handlers and layout; Gum draws it and takes the input (the gamepad by Gum's focus).
/// </summary>
internal sealed class BestiaryScreen : ContainerRuntime, IGumScreenUpdate
{
    public static readonly Func<bool> Shows = GumRoutes.State<UIBestiaryTest>();

    public static BestiaryScreen? Instance { get; private set; }

    private readonly GameUiMirror _mirror = new();

    public BestiaryScreen()
    {
        Instance = this;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        Children.Add(FreshStyle.Backdrop(FreshStyle.Dim));
        Children.Add(_mirror);
    }

    public Gum.Forms.Controls.FrameworkElement? First => _mirror.First;

    public void UpdateScreen()
    {
        _mirror.Source = Main.inFancyUI ? Main.InGameUI.CurrentState as UIBestiaryTest : null;
        _mirror.Update();
    }
}
