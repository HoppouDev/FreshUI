using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Terraria;
using Terraria.GameContent.UI.Elements;
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

    // An entry's size, in UI units (two a UI pixel), and the gap between entries: a UI pixel, the art's.
    private const float Entry = 72, EntryGap = 2;
    private static readonly System.Reflection.FieldInfo Grid = typeof(UIBestiaryTest).GetField("_entryGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    private UIElement? _laidOut;

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

    // The game spreads its entries over the grid's space by fractions of it, so they land on fractional units, their
    // layered art (back, frame, overlay) a screen pixel apart and the gaps uneven: they go on whole UI pixels instead,
    // a UI pixel apart (none where the space is too tight), the block centred. Each time the grid fills anew.
    private void LayOutEntries(UIBestiaryEntryGrid grid)
    {
        List<UIElement> entries = grid.Children.ToList();
        if (entries.Count == 0 || entries[0] == _laidOut)
            return;
        _laidOut = entries[0];
        grid.GetEntriesToShow(out int columns, out int rows, out _);
        columns = Math.Max(1, columns);
        rows = Math.Max(1, rows);
        CalculatedStyle area = grid.GetInnerDimensions();
        float gapX = area.Width - columns * Entry >= (columns - 1) * EntryGap ? EntryGap : 0;
        float gapY = area.Height - rows * Entry >= (rows - 1) * EntryGap ? EntryGap : 0;
        static float Even(float units) => MathF.Round(units / 2) * 2;
        float left = Even(area.X + (area.Width - columns * Entry - (columns - 1) * gapX) / 2) - area.X;
        float top = Even(area.Y + (area.Height - rows * Entry - (rows - 1) * gapY) / 2) - area.Y;
        for (int i = 0; i < entries.Count; i++)
        {
            UIElement entry = entries[i];
            entry.HAlign = entry.VAlign = 0;
            entry.Left.Set(left + i % columns * (Entry + gapX), 0);
            entry.Top.Set(top + i / columns * (Entry + gapY), 0);
            entry.Recalculate();
        }
    }

    public void UpdateScreen()
    {
        var bestiary = Main.inFancyUI ? Main.InGameUI.CurrentState as UIBestiaryTest : null;
        if (bestiary != null)
            LayOutEntries((UIBestiaryEntryGrid)Grid.GetValue(bestiary)!);
        _mirror.Source = bestiary;
        _mirror.Update();
    }
}
