using FreshUI.GumUi;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace FreshUI.Components;

/// <summary>
/// A horizontal pill (the switch and slider tracks): its art's rounded left end, middle stretched across
/// and right end, three sprites each the art's full height, at half a UI pixel per texel. In place of a
/// nine-slice, which also slices the art across its height and places the bottom slices a screen pixel
/// off, so the ends' lower halves don't meet the rest.
/// </summary>
sealed class FreshPill : ContainerRuntime
{
    private const float Texel = 0.5f;

    private readonly int _end;
    private readonly SpriteRuntime _left;
    private readonly SpriteRuntime _middle;
    private readonly SpriteRuntime _right;

    /// <param name="end">Texels of the art at each end that keep their size (the rounded part).</param>
    public FreshPill(int end)
    {
        _end = end;
        HasEvents = false;
        _left = Part();
        _middle = Part();
        _right = Part();
        _left.Width = _right.Width = end * Texel;
        _middle.X = end * Texel;
        _middle.WidthUnits = DimensionUnitType.RelativeToParent;
        _middle.Width = -2 * end * Texel;
        _right.XUnits = GeneralUnitType.PixelsFromLarge;
        _right.XOrigin = HorizontalAlignment.Right;
        Children.Add(_left);
        Children.Add(_middle);
        Children.Add(_right);
    }

    private static SpriteRuntime Part() => new()
    {
        TextureAddress = TextureAddress.Custom,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.RelativeToParent,
        Height = 0,
    };

    public Texture2D? Texture
    {
        get => _left.Texture;
        set
        {
            if (value == null || value == _left.Texture)
                return;
            int width = value.Width, height = value.Height;
            Show(_left, value, 0, _end, height);
            Show(_middle, value, _end, width - 2 * _end, height);
            Show(_right, value, width - _end, _end, height);
        }
    }

    public Color Color
    {
        get => _left.Color;
        set => _left.Color = _middle.Color = _right.Color = value;
    }

    private static void Show(SpriteRuntime part, Texture2D texture, int left, int width, int height)
    {
        part.Texture = texture;
        part.TextureLeft = left;
        part.TextureTop = 0;
        part.TextureWidth = width;
        part.TextureHeight = height;
    }
}
