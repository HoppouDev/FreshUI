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
/// Each row is a <see cref="FreshPanel"/> its screen fills (<see cref="Row"/>, <see cref="Name"/>, <see cref="Icon"/>).
/// </summary>
internal abstract class SelectScreen : ContainerRuntime, IGumScreenUpdate
{
    protected const float WindowWidth = 325, RowHeight = 48, Gap = 4, ButtonHeight = 25, Top = 110;

    private readonly FreshWindow _window;
    private readonly FreshPanel _panel = new();
    private readonly FreshPanelButton _back, _new;
    private readonly Action _goBack;

    /// <summary>The list's rows.</summary>
    protected readonly FreshScrollViewer List = new() { WheelStep = RowHeight + Gap, IsFocusable = false };

    /// <summary>Each row's first button (the gamepad's focus starts at the first).</summary>
    protected readonly List<FreshIconButton> Plays = [];

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
        List.Visual.X = List.Visual.Y = 6;
        List.Visual.WidthUnits = List.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        List.Visual.Width = List.Visual.Height = -12;
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

    /// <summary>Clears the rows (before adding them again).</summary>
    protected void ClearRows()
    {
        List.InnerPanel.Children.Clear();
        Plays.Clear();
    }

    /// <summary>A new row, added to the list.</summary>
    protected FreshPanel Row()
    {
        var row = new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = RowHeight };
        List.InnerPanel.Children.Add(row);
        return row;
    }

    /// <summary>A row's name, top left after its picture.</summary>
    protected static TextRuntime Name(FreshPanel row, string text, Color color)
    {
        var name = new TextRuntime
        {
            X = 38,
            Y = 3,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 10,
            Text = text,
            Color = color,
            HasEvents = false,
        };
        GumText.SetOutline(name, 0.75f, Color.Black);
        row.Children.Add(name);
        return name;
    }

    /// <summary>
    /// One of a row's image buttons along its bottom, as FreshUI draws the game's list buttons: just the icon (its
    /// button's frame and fill dropped), a texel a UI pixel, gold while hovered; its name as its tooltip.
    /// </summary>
    protected static FreshIconButton Icon(FreshPanel row, ref float x, string texture, string name, Action onClick)
    {
        Texture2D art = FreshUI.Legacy.PixelImages.Icon(GumTextures.Load(texture));
        var button = new FreshIconButton { Look = new FreshIconButtonLook(art), TexelSize = 1, HoverColor = FreshStyle.Gold };
        button.Visual.Width = art.Width;
        button.Visual.Height = art.Height;
        button.Visual.X = x;
        button.Visual.Y = RowHeight - 3 - art.Height;
        button.Click += (_, _) => onClick();
        // Its name for automation too (the tree's label).
        button.Visual.Name = name;
        FreshTooltip.Attach(button.Visual, () => name);
        row.Children.Add(button.Visual);
        x += art.Width;
        return button;
    }

    /// <summary>Where a row's delete button goes: its right end.</summary>
    protected const float DeleteX = WindowWidth - 12 - 6 - 26;
}
