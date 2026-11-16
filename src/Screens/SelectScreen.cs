using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// A title menu's list to choose from (the player select, the world select): a <see cref="FreshWindow"/> under the
/// game's logo with a row per entry in a <see cref="FreshScrollViewer"/>, and Back and New under it, B going back.
/// Each row is a <see cref="FreshPanel"/> its screen fills, laid out alike: the entry's picture (drawn by the game,
/// <see cref="ArtSize"/> square) at the top left, its <see cref="Name"/> beside it with its <see cref="Details"/>
/// under that and an <see cref="Aside"/> at the right, and its buttons (<see cref="Icon"/>, <see cref="Delete"/>)
/// on a line under the picture. A row is lit (a gold border) while the cursor is over it or one of its buttons has
/// the focus, so it's clear which entry a button acts on.
/// </summary>
internal abstract class SelectScreen : ContainerRuntime, IGumScreenUpdate
{
    protected const float WindowWidth = 325, Gap = 4, ButtonHeight = 25, Top = 110;

    /// <summary>
    /// A row's layout, in UI pixels from its outer edge (its border is one): the same gap (5) inside the border at
    /// the top and sides, and a wider band for the buttons' glyphs under the picture. The picture is <see cref="ArtSize"/>
    /// square at the top left; the text beside it, the name level with its top and the details line ending level with
    /// its bottom; the button line under it.
    /// </summary>
    protected const float Inset = 6, ArtSize = 29, TextX = Inset + ArtSize + 6, DetailsY = Inset + ArtSize - 6;

    // The buttons' art (22 square) keeps a few pixels' margin around its glyph (11 to 14 tall, the trash can the
    // tallest): the line starts that much early, so the glyphs line up with the picture (and delete's with the
    // aside), and its band sits 1 under the picture and 4 over the border, the glyphs about evenly clear above and below.
    private const float ArtMargin = 4, ButtonArt = 22;

    protected const float ButtonsY = Inset + ArtSize + 1, RowHeight = ButtonsY + ButtonArt + 4;

    /// <summary>Where a row's first button goes.</summary>
    protected const float ButtonsX = Inset - ArtMargin;

    private readonly FreshWindow _window;
    private readonly FreshPanel _panel = new();
    private readonly FreshPanelButton _back, _new;
    private readonly Action _goBack;

    /// <summary>The list's rows.</summary>
    protected readonly FreshScrollViewer List = new() { WheelStep = RowHeight + Gap, IsFocusable = false };

    /// <summary>Each row's first button (the gamepad's focus starts at the first).</summary>
    protected readonly List<FreshIconButton> Plays = [];

    private readonly List<FreshPanel> _rows = [];

    protected SelectScreen(string title, Action goBack, string newText, Action onNew)
    {
        _goBack = goBack;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        InteractiveGue zone = GumGamepad.SpatialZone();
        Children.Add(zone);
        _window = new FreshWindow(title);
        zone.Children.Add(_window);
        _panel.WidthUnits = _panel.HeightUnits = DimensionUnitType.RelativeToParent;
        _panel.Width = _panel.Height = 0;
        _window.Add(_panel);
        // The view to the panel's outer edge (clipped there), its rows and bar 6 in from it.
        (List.Visual.X, List.Visual.Y) = (6, -1);
        List.Visual.WidthUnits = List.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        (List.Visual.Width, List.Visual.Height) = (-12, 2);
        List.SetContentInset(7, 7);
        List.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        List.InnerPanel.StackSpacing = Gap;
        _panel.Children.Add(List.Visual);
        _back = Button(Terraria.Localization.Language.GetTextValue("UI.Back"), goBack);
        _new = Button(newText, onNew);
        zone.Children.Add(_back.Visual);
        zone.Children.Add(_new.Visual);
    }

    /// <summary>The first row's first button, else New.</summary>
    public FrameworkElement First => Plays.Count > 0 ? Plays[0] : _new;

    /// <summary>Each update while it shows: the rows, rebuilt when what they show changes.</summary>
    protected abstract void UpdateRows();

    public void UpdateScreen()
    {
        UpdateRows();
        LightRows();
        float canvasWidth = GraphicalUiElement.CanvasWidth, canvasHeight = GraphicalUiElement.CanvasHeight;
        float height = MathF.Floor(canvasHeight - Top - 20 - ButtonHeight - Gap);
        float x = MathF.Floor((canvasWidth - WindowWidth) / 2);
        if (_window.X != x || _window.Height != height)
        {
            (_window.X, _window.Y, _window.Width, _window.Height) = (x, Top, WindowWidth, height);
            _window.Layout();
            float half = MathF.Floor((WindowWidth - Gap) / 2);
            (_back.Visual.X, _back.Visual.Y, _back.Visual.Width) = (x, Top + height + Gap, half);
            (_new.Visual.X, _new.Visual.Y, _new.Visual.Width) = (x + WindowWidth - half, Top + height + Gap, half);
        }
        // B (the inventory key: the menu's back on the title menu) goes back.
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            _goBack();
        }
    }

    private static FreshPanelButton Button(string text, Action onClick)
    {
        var button = new FreshPanelButton { Text = text };
        button.Visual.WidthUnits = button.Visual.HeightUnits = DimensionUnitType.Absolute;
        button.Visual.Height = ButtonHeight;
        button.Click += (_, _) => onClick();
        return button;
    }

    // The row the cursor is over (within the list's view) or whose button has the focus is lit.
    private void LightRows()
    {
        Gum.Wireframe.ICursor cursor = FrameworkElement.MainCursor;
        float x = cursor.XRespectingGumZoomAndBounds(), y = cursor.YRespectingGumZoomAndBounds();
        bool inList = GumHost.Contains(List.Visual, x, y);
        object? focus = InteractiveGue.CurrentInputReceiver;
        foreach (FreshPanel row in _rows)
        {
            bool lit = inList && GumHost.Contains(row, x, y) || focus is FrameworkElement { Visual.Parent: var parent } && parent == row;
            Color border = lit ? FreshStyle.Gold : Color.Black;
            if (row.BorderColor != border)
                (row.BorderColor, row.BackgroundColor) = (border, lit ? FreshStyle.Button : FreshStyle.Panel);
        }
    }

    /// <summary>Clears the rows (before adding them again).</summary>
    protected void ClearRows()
    {
        List.InnerPanel.Children.Clear();
        Plays.Clear();
        _rows.Clear();
    }

    /// <summary>A new row, added to the list.</summary>
    protected FreshPanel Row()
    {
        var row = new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = RowHeight };
        List.InnerPanel.Children.Add(row);
        _rows.Add(row);
        return row;
    }

    /// <summary>Where a row's picture is drawn (in the game's units, two a UI pixel), from the row's area in them.</summary>
    protected static Vector2 ArtPosition(Rectangle area) => new(area.X + Inset * 2, area.Y + Inset * 2);

    /// <summary>A row's name, beside its picture.</summary>
    protected static TextRuntime Name(FreshPanel row, string text, Color color)
    {
        TextRuntime name = Text(text, 10, color);
        (name.X, name.Y) = (TextX, Inset + 1);
        row.Children.Add(name);
        return name;
    }

    /// <summary>A row's details under its name, side by side (<see cref="Detail"/>s, <see cref="DetailIcon"/>s).</summary>
    protected static void Details(FreshPanel row, params GraphicalUiElement[] parts)
    {
        var line = new ContainerRuntime
        {
            X = TextX,
            Y = DetailsY,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack,
            StackSpacing = 3,
            HasEvents = false,
        };
        foreach (GraphicalUiElement part in parts)
            line.Children.Add(part);
        row.Children.Add(line);
    }

    /// <summary>A detail's text, smaller than the name; a wider gap before it than an icon's.</summary>
    protected static TextRuntime Detail(string text, Color color, bool spaced = false)
    {
        TextRuntime detail = Text(text, 8.5f, color);
        // A stack's spacing is between every part: the gap before a new group is its own.
        if (spaced)
            detail.X = 5;
        return detail;
    }

    /// <summary>A small picture before a detail (the life and mana icons): the game's 2×2-texel art, centred on the text.</summary>
    protected static SpriteRuntime DetailIcon(string texture, bool spaced = false)
    {
        Texture2D art = GumTextures.Load(texture);
        return new SpriteRuntime
        {
            Texture = art,
            X = spaced ? 5 : 0,
            // Centred on the capitals, about 3.5 below the detail text's top.
            Y = MathF.Round(3.5f - art.Height / 4f),
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = art.Width / 2f,
            Height = art.Height / 2f,
        };
    }

    /// <summary>A row's aside: muted text at the right of its details line (a world's creation date, a player's time played).</summary>
    protected static void Aside(FreshPanel row, string text)
    {
        TextRuntime aside = Text(text, 8.5f, FreshStyle.MutedText);
        aside.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        aside.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        (aside.X, aside.Y) = (-Inset, DetailsY);
        row.Children.Add(aside);
    }

    private static TextRuntime Text(string text, float size, Color color)
    {
        var runtime = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = size,
            Text = text,
            Color = color,
            HasEvents = false,
        };
        GumText.SetOutline(runtime, 0.75f, Color.Black);
        return runtime;
    }

    /// <summary>
    /// One of a row's image buttons on its button line, as FreshUI draws the game's list buttons: just the icon (its
    /// button's frame and fill dropped), a texel a UI pixel, a little dim at rest and backed by the lit panel while
    /// hovered or focused, its name as its tooltip (under it while focused).
    /// </summary>
    protected static FreshIconButton Icon(FreshPanel row, ref float x, string texture, string name, Action onClick)
    {
        Texture2D art = FreshUI.Hooks.PixelImages.Icon(GumTextures.Load(texture));
        var button = new FreshIconButton { Look = new FreshIconButtonLook(art), TexelSize = 1, IdleColor = new Color(205, 205, 205), HasBacking = true };
        button.Visual.Width = art.Width;
        button.Visual.Height = art.Height;
        button.Visual.X = x;
        button.Visual.Y = ButtonsY;
        button.Click += (_, _) => onClick();
        // Its name for automation too (the tree's label).
        button.Visual.Name = name;
        FreshTooltip.Attach(button, () => name);
        row.Children.Add(button.Visual);
        x += art.Width;
        return button;
    }

    /// <summary>A row's delete button, at the right end of its button line.</summary>
    protected static FreshIconButton Delete(FreshPanel row, Action onClick)
    {
        float x = 0;
        FreshIconButton delete = Icon(row, ref x, "Images/UI/ButtonDelete", Terraria.Localization.Language.GetTextValue("UI.Delete"), onClick);
        delete.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        delete.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        delete.Visual.X = -ButtonsX;
        return delete;
    }
}
