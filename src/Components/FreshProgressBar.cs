using FreshUI.GumUi;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace FreshUI.Components;

/// <summary>
/// How far along something is (an invasion, a world's generation): the slider's track (the scrollbar pill,
/// 9 pixels tall) filled from its left in the slider's teal up to <see cref="Value"/> (0 to 1), on whole screen
/// pixels. Takes no input.
/// </summary>
class FreshProgressBar : ContainerRuntime
{
    private const int Corner = 6;
    public const float BarHeight = 9;

    private readonly FreshPill _track, _fill;
    private float _opacity = 1;
    private Color _fillColor = Color.White;
    private float _value = -1;

    public FreshProgressBar()
    {
        HasEvents = false;
        Height = BarHeight;
        _track = Pill(FreshToggleSwitch.TrackOffPath, fill: false);
        Children.Add(_track);
        _fill = Pill(FreshToggleSwitch.TrackOnPath, fill: true);
        Children.Add(_fill);
        Value = 0;
    }

    private static FreshPill Pill(string texture, bool fill) => new(Corner)
    {
        Texture = FreshToggleSwitch.Track(texture),
        WidthUnits = fill ? DimensionUnitType.Absolute : DimensionUnitType.RelativeToParent,
        HeightUnits = DimensionUnitType.Absolute,
        Width = 0,
        Height = BarHeight,
        YUnits = GeneralUnitType.PixelsFromMiddle,
        YOrigin = VerticalAlignment.Center,
    };

    /// <summary>How far along, 0 to 1.</summary>
    public float Value
    {
        get => _value;
        set
        {
            value = Math.Clamp(value, 0, 1);
            if (value == _value)
                return;
            _value = value;
            Refresh();
        }
    }

    /// <summary>Tints the fill (white: the slider's teal).</summary>
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            ApplyColors();
        }
    }

    /// <summary>How opaque the whole bar is (fading in and out).</summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            if (_opacity == value)
                return;
            _opacity = value;
            ApplyColors();
        }
    }

    private void ApplyColors()
    {
        _track.Color = Color.White * _opacity;
        _fill.Color = _fillColor * _opacity;
    }

    /// <summary>Lays the fill out again (after the bar's width changes).</summary>
    public void Refresh()
    {
        float width = GetAbsoluteWidth();
        // Nothing until there's room for the pill's rounded ends.
        float fill = _value <= 0 ? 0 : MathF.Max(Corner, UiPixels.Snap(width * _value));
        _fill.Visible = fill > 0;
        if (_fill.Width != fill)
            _fill.Width = fill;
    }
}
