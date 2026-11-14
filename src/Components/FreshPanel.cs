using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;

namespace FreshUI.Components;

/// <summary>
/// The game's panel (as its windows and the Mods window draw them): the panel background in
/// <see cref="BackgroundColor"/> under its border in <see cref="BorderColor"/>, nine-sliced to any size.
/// A container: add its content as children.
/// </summary>
class FreshPanel : ContainerRuntime
{
    private readonly NineSliceRuntime _background;
    private readonly NineSliceRuntime _border;

    public FreshPanel()
    {
        HasEvents = false;
        _background = Slice(FreshStyle.PanelBackground, FreshStyle.Panel);
        _border = Slice(FreshStyle.PanelBorder, Color.Black);
        Children.Add(_background);
        Children.Add(_border);
    }

    public Color BackgroundColor
    {
        get => _background.Color;
        set => _background.Color = value;
    }

    public Color BorderColor
    {
        get => _border.Color;
        set => _border.Color = value;
    }

    /// <summary>The panel art filling its parent, its corners kept whole.</summary>
    internal static NineSliceRuntime Slice(string texture, Color color) => new()
    {
        Texture = GumTextures.Load(texture),
        TextureAddress = TextureAddress.EntireTexture,
        CustomFrameTextureCoordinateWidth = FreshStyle.PanelCorner,
        BorderScale = FreshStyle.Texel,
        WidthUnits = DimensionUnitType.RelativeToParent,
        HeightUnits = DimensionUnitType.RelativeToParent,
        Width = 0,
        Height = 0,
        Color = color,
        HasEvents = false,
    };
}
