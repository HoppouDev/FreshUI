using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
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

    // An entry's size, in UI units (two a UI pixel), and the gaps between entries: a UI pixel across, two down (the
    // slot art has a pixel of shadow under it).
    private const float Entry = 72, EntryGapX = 2, EntryGapY = 4;
    // The top bar's height (the game's), the arrows' width, the gap between the bar's items, Back's height and its gap
    // under the window, in UI units.
    private const float BarHeight = 24, ArrowWidth = 24, BarGap = 2, BackHeight = 50, BackGap = 8;
    private static readonly System.Reflection.FieldInfo Grid = Field("_entryGrid"), Space = Field("_bestiarySpace"),
        RangeText = Field("_indexesRangeText"), Progress = Field("_unlocksProgressBar");
    private UIElement? _laidOut;
    private UIBestiaryTest? _arranged;

    private static System.Reflection.FieldInfo Field(string name) =>
        typeof(UIBestiaryTest).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

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

    // The game's page, in the menus' style (once: the game builds it once): no unlocks bar under the grid (the grid
    // takes its place); the top bar's items all its height, the range's readout as a field, the page arrows panel buttons (a label each, which the
    // mirror makes them) and the search field's own button gone (a click on the field searches); Back as wide as the
    // window, under it as the menus' actions are.
    private static void Arrange(UIBestiaryTest bestiary)
    {
        var space = (UIElement)Space.GetValue(bestiary)!;
        UIElement content = space.Parent, panel = content.Parent, window = panel.Parent;
        var progress = (UIElement)Progress.GetValue(bestiary)!;
        panel.RemoveChild(progress.Parent);
        content.Top.Set(0, 0);
        content.Height.Set(-BarHeight - 6, 1);
        panel.Height.Set(-BackHeight - BackGap, 1);
        foreach (UIElement child in window.Children)
        {
            if (child != panel)
            {
                (child.Left, child.Top, child.HAlign) = (StyleDimension.Empty, StyleDimension.Empty, 0.5f);
                child.Width.Set(0, 1);
                child.Height.Set(BackHeight, 0);
            }
        }
        var range = (UIPanel)((UIElement)RangeText.GetValue(bestiary)!).Parent;
        // The range's readout in the search field's look (the game's is flat, its border its fill).
        (range.BackgroundColor, range.BorderColor) = (FreshStyle.Button, Color.Black);
        UIElement bar = range.Parent;
        float x = 0;
        foreach (UIElement item in bar.Children.ToList())
        {
            item.Top.Set(0, 0);
            item.VAlign = 0;
            item.Height.Set(0, 1);
            if (item is UIWrappedSearchBar search)
                search.HideSearchButton();
            else if (item.HAlign == 0)
            {
                // The arrows, then the range's panel, left to right.
                if (item is UIImageButton arrow)
                {
                    arrow.Width.Set(ArrowWidth, 0);
                    arrow.Append(new UIText(x == 0 ? "<" : ">", 0.8f));
                }
                item.Left.Set(x, 0);
                x += item.Width.Pixels + BarGap;
            }
        }
        window.Recalculate();
    }

    // The game spreads its entries over the grid's space by fractions of it, so they land on fractional units, their
    // layered art (back, frame, overlay) a screen pixel apart and the gaps uneven: they go on whole UI pixels instead,
    // a UI pixel apart across and two down (none where the space is too tight), the block centred. Each time the grid fills anew.
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
        float gapX = area.Width - columns * Entry >= (columns - 1) * EntryGapX ? EntryGapX : 0;
        float gapY = area.Height - rows * Entry >= (rows - 1) * EntryGapY ? EntryGapY : 0;
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
        {
            if (bestiary != _arranged)
                Arrange(_arranged = bestiary);
            LayOutEntries((UIBestiaryEntryGrid)Grid.GetValue(bestiary)!);
        }
        _mirror.Source = bestiary;
        _mirror.Update();
    }
}
