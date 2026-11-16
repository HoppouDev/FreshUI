using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;

namespace FreshUI.Components;

/// <summary>
/// The look every component shares: the Mods window's palette (Aether's, which FreshUI's menus have always
/// used), the game's gold for hover and focus, and the game's panel art (2×2 texels a UI pixel).
/// </summary>
public static class FreshStyle
{
    /// <summary>A window's panels.</summary>
    public static Color Panel => new Color(33, 43, 79) * 0.85f;

    /// <summary>A button at rest.</summary>
    public static Color Button => new Color(63, 82, 151) * 0.8f;

    /// <summary>A button hovered, focused or chosen (an open tab), and a window's title.</summary>
    public static Color ButtonLit => new(73, 94, 171);

    /// <summary>Secondary text: group headers, descriptions.</summary>
    public static Color MutedText => new(188, 204, 240);

    /// <summary>The game's hovered and selected UI colour: borders, focus, knobs.</summary>
    public static Color Gold => new(255, 231, 69);

    /// <summary>Disabled text and art.</summary>
    public static Color Disabled => new(130, 130, 130);

    /// <summary>The game dimmed behind a menu that has it to itself (the pause menu, the settings in game).</summary>
    public static Color Dim => Color.Black * 0.5f;

    /// <summary>A rectangle filling its parent in <paramref name="color"/>: a screen's backdrop.</summary>
    public static RectangleRuntime Backdrop(Color color) => new()
    {
        WidthUnits = DimensionUnitType.RelativeToParent,
        HeightUnits = DimensionUnitType.RelativeToParent,
        Width = 0,
        Height = 0,
        IsFilled = true,
        StrokeColor = Color.Transparent,
        FillColor = color,
    };

    public const string PanelBackground = "Images/UI/PanelBackground";
    public const string PanelBorder = "Images/UI/PanelBorder";

    /// <summary>The panel art's rounded corners, in texels.</summary>
    public const int PanelCorner = 12;

    /// <summary>UI pixels per texel of the game's 2×2-texel art.</summary>
    public const float Texel = 0.5f;
}
