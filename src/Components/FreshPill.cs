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
/// A horizontal pill (the switch, slider and progress bar tracks): its art's rounded left end, middle stretched across
/// and right end, three sprites each the art's full height, at <c>texel</c> UI pixels per texel. In place of a
/// nine-slice, which also slices the art across its height and places the bottom slices a screen pixel
/// off, so the ends' lower halves don't meet the rest.
/// </summary>
public sealed class FreshPill : ContainerRuntime
{
    private readonly float _texel;
    private readonly int _end;
    private readonly SpriteRuntime _left;
    private readonly SpriteRuntime _middle;
    private readonly SpriteRuntime _right;

    /// <param name="end">Texels of the art at each end that keep their size (the rounded part).</param>
    /// <param name="texel">UI pixels a texel: half for the game's 2×2-texel art, 1 for FreshUI's own.</param>
    public FreshPill(int end, float texel = 0.5f)
    {
        _end = end;
        _texel = texel;
        HasEvents = false;
        _left = Part();
        _middle = Part();
        _right = Part();
        _left.Width = _right.Width = end * _texel;
        _middle.X = end * _texel;
        _middle.WidthUnits = DimensionUnitType.RelativeToParent;
        _middle.Width = -2 * end * _texel;
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

    /// <summary>
    /// Whether its right end is square (its middle running to the edge), as a slider's fill under the round knob: a
    /// rounded end there curves away from the knob in its rim rows, showing the empty track between them.
    /// </summary>
    public bool SquareRight
    {
        get => !_right.Visible;
        set
        {
            _right.Visible = !value;
            _middle.Width = (value ? -1 : -2) * _end * _texel;
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
