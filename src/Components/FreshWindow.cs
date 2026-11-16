using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;

namespace FreshUI.Components;

/// <summary>
/// A window as FreshUI's menus draw them (the pause menu, the settings, the Mods window): its panels
/// (<see cref="Add"/>, laid out by the screen) under a title tab in <see cref="FreshStyle.ButtonLit"/>
/// that sits over the window's top edge, centred on whole pixels (<see cref="Layout"/>).
/// </summary>
class FreshWindow : ContainerRuntime
{
    /// <summary>How far the title tab's top is above the window's.</summary>
    public const float TitleLift = 14;
    private const float TitlePadding = 6;

    private readonly FreshPanel _tab;
    private readonly FreshLabel _title;

    public FreshWindow(string title)
    {
        HasEvents = false;
        _tab = new FreshPanel
        {
            BackgroundColor = FreshStyle.ButtonLit,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = TitlePadding,
            Height = TitlePadding,
            Y = -TitleLift,
        };
        _title = new FreshLabel { Text = title, FontSize = 16, OutlineWidth = 0.75f };
        _title.Visual.X = TitlePadding;
        _title.Visual.Y = TitlePadding;
        _tab.Children.Add(_title.Visual);
        Children.Add(_tab);
    }

    public string Title
    {
        get => _title.Text ?? "";
        set => _title.Text = value;
    }

    /// <summary>Adds a panel (or anything else), under the title tab.</summary>
    public void Add(GraphicalUiElement element) => Children.Insert(Children.IndexOf(_tab), element);

    // A menu window's place, in UI pixels (as the settings window's): on the title menu under the logo down to near
    // the bottom; in game a shorter one, centred.
    private const float MenuWidth = 480, GameHeight = 320, GameMargin = 24, MenuTop = 109, MenuMargin = 22;

    /// <summary>
    /// Places the window as FreshUI's menu windows (the settings, the Mods window): up to <paramref name="maxWidth"/>
    /// wide (and 88% of the screen), centred; returns whether its size or place changed.
    /// </summary>
    public bool PlaceAsMenu(bool inGame, float maxWidth = MenuWidth)
    {
        float canvasWidth = GraphicalUiElement.CanvasWidth, canvasHeight = GraphicalUiElement.CanvasHeight;
        float width = MathF.Floor(MathF.Min(maxWidth, canvasWidth * 0.88f)), height, top;
        if (inGame)
        {
            height = MathF.Floor(MathF.Min(GameHeight, canvasHeight - GameMargin * 2));
            top = MathF.Floor((canvasHeight - height) / 2 + GameMargin / 4);
        }
        else
        {
            top = MenuTop;
            height = MathF.Floor(canvasHeight - MenuTop - MenuMargin);
        }
        float x = MathF.Floor((canvasWidth - width) / 2);
        if (X == x && Y == top && Width == width && Height == height)
            return false;
        (X, Y, Width, Height) = (x, top, width, height);
        Layout();
        return true;
    }

    /// <summary>Centres the title tab on whole pixels, once the window's width is set (each update, as the title's width can change).</summary>
    public void Layout() => _tab.X = MathF.Floor((AbsoluteWidth - _tab.AbsoluteWidth) / 2);
}
