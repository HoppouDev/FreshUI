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
    public const float TitleLift = 20;
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

    /// <summary>Centres the title tab on whole pixels, once the window's width is set (each update, as the title's width can change).</summary>
    public void Layout() => _tab.X = MathF.Floor((AbsoluteWidth - _tab.AbsoluteWidth) / 2);
}
