using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;

namespace FreshUI.Components;

/// <summary>
/// A horizontal rule between groups in a panel, as the world creation's between its rows of options: the
/// game's separator art (its ends kept, its middle stretched) as wide as its parent, in a pale panel blue.
/// </summary>
class FreshSeparator : ContainerRuntime
{
    public static string TexturePath { get; set; } = "Images/UI/CharCreation/Separator1";

    // The art's ends that keep their size, in texels, and its colour in the world creation.
    private const int End = 2;
    public static readonly Color DefaultColor = Color.Lerp(Color.White, new Color(63, 65, 151), 0.85f) * 0.9f;

    public FreshSeparator()
    {
        HasEvents = false;
        var texture = GumTextures.Load(TexturePath);
        WidthUnits = DimensionUnitType.RelativeToParent;
        Width = 0;
        Height = texture.Height * FreshStyle.Texel;
        Children.Add(new FreshPill(End)
        {
            Texture = texture,
            Color = DefaultColor,
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
        });
    }
}
