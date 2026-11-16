using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Components;

/// <summary>
/// A colour as hue, saturation and lightness: three <see cref="FreshSlider"/> colour bars, each track showing
/// the colours it would give (as the game's HSL bars: the stylist's, the dresser's, the character creation's).
/// It edits a colour kept elsewhere: <see cref="Get"/> reads it (each <see cref="Sync"/>, every update while it
/// shows) and <see cref="Set"/> takes a bar's change. Lightness stays at or above <see cref="MinLightness"/>,
/// its bar spanning the rest.
/// </summary>
public class FreshColorPicker : ContainerRuntime
{
    public const float BarWidth = 84, RowHeight = 13;

    private readonly FreshSlider[] _bars = new FreshSlider[3];
    private bool _syncing;
    private Vector3 _hsl;

    public FreshColorPicker()
    {
        HasEvents = false;
        Width = BarWidth;
        Height = 2 * RowHeight + 9;
        for (int i = 0; i < 3; i++)
        {
            int row = i;
            var bar = new FreshSlider { Minimum = 0, Maximum = 1 };
            bar.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            bar.Visual.Width = 0;
            bar.Visual.Y = row * RowHeight;
            bar.Ramp = t => Main.hslToRgb(row == 0 ? t : _hsl.X, row == 1 ? t : _hsl.Y, row == 2 ? Lightness(t) : _hsl.Z);
            bar.ValueChanged += (_, _) =>
            {
                if (_syncing)
                    return;
                float value = (float)bar.Value;
                Vector3 hsl = _hsl;
                switch (row)
                {
                    case 0: hsl.X = value; break;
                    case 1: hsl.Y = value; break;
                    default: hsl.Z = Lightness(value); break;
                }
                _hsl = hsl;
                Set?.Invoke(hsl);
                UpdateRamps();
            };
            _bars[i] = bar;
            Children.Add(bar.Visual);
        }
    }

    /// <summary>Reads the colour being edited, as hue, saturation and lightness (0 to 1).</summary>
    public Func<Vector3>? Get { get; set; }

    /// <summary>Takes the colour as a bar changes it.</summary>
    public Action<Vector3>? Set { get; set; }

    /// <summary>The least lightness (the game's bars keep 0.15: no black hair or skin).</summary>
    public float MinLightness { get; set; }

    /// <summary>The hue, saturation and lightness bars.</summary>
    public IReadOnlyList<FreshSlider> Bars => _bars;

    private float Lightness(float bar) => bar * (1 - MinLightness) + MinLightness;

    /// <summary>Shows the colour as <see cref="Get"/> has it now.</summary>
    public void Sync()
    {
        if (Get == null)
            return;
        Vector3 hsl = Get();
        if (hsl == _hsl)
            return;
        _hsl = hsl;
        _syncing = true;
        SetBar(0, hsl.X);
        SetBar(1, hsl.Y);
        SetBar(2, MinLightness >= 1 ? 0 : Math.Clamp((hsl.Z - MinLightness) / (1 - MinLightness), 0, 1));
        _syncing = false;
        UpdateRamps();
    }

    private void SetBar(int index, float value)
    {
        if (MathF.Abs((float)_bars[index].Value - value) > 1e-4f)
            _bars[index].Value = value;
    }

    private void UpdateRamps()
    {
        foreach (FreshSlider bar in _bars)
            bar.UpdateRamp();
    }
}
