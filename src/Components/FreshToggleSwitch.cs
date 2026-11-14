using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Components;

/// <summary>
/// An on/off switch: a pill-shaped track (the game's scrollbar art, blue off and teal on) with a round
/// knob (the settings toggle's filled circle) that slides across when it flips. A Forms
/// <see cref="ToggleButton"/>, so <see cref="ToggleButton.IsChecked"/>, its events, keyboard focus and
/// remote automation work as for any toggle. Hovered or focused (by the gamepad), the knob is gold.
/// Sizes are in UI pixels; the art is 2×2 texels a pixel.
/// </summary>
class FreshToggleSwitch : ToggleButton
{
    public static string TrackOffPath { get; set; } = "Images/UI/Scrollbar";
    public static string TrackOnPath { get; set; } = "Images/UI/Scrollbar2";
    public static string KnobPath { get; set; } = "Images/UI/Settings_Toggle";

    // The track's art is 20×16 texels with rounded ends 6 texels deep; the knob is the filled circle,
    // 14 texels across, at the right of the toggle art.
    private const int TrackCorner = 6;
    internal static readonly Rectangle KnobSource = new(16, 0, 14, 14);
    private const float Texel = 0.5f;
    private const float KnobSize = 14 * Texel;

    /// <summary>
    /// The track art (16 texels tall, 8 pixels) with an interior pixel row repeated: 9 pixels, the
    /// height the 7-pixel knob sits in a pixel from each edge, drawn without stretching. (A nine-slice
    /// stretching the art's 2-pixel middle to 3 doubles one row unevenly, shifting the rounded ends.)
    /// </summary>
    internal static Texture2D Track(string path) => GumTextures.Taller(path, 6, 2);

    // The knob's slide: the share of the way to its target covered per second, roughly.
    private const float SlideSpeed = 18f;

    private readonly FreshPill _track;
    private readonly SpriteRuntime _knob;
    private float _knobAt;
    private bool _hovered, _pushed, _enabled = true;

    public FreshToggleSwitch()
    {
        var visual = (ToggleButtonVisual)Visual;
        visual.Background.Visible = false;
        visual.TextInstance.Visible = false;
        visual.WidthUnits = DimensionUnitType.Absolute;
        visual.HeightUnits = DimensionUnitType.Absolute;
        // The 7-pixel knob a whole pixel inside the track all round.
        visual.Width = 18;
        visual.Height = 9;

        _track = new FreshPill(TrackCorner)
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
        };
        _knob = new SpriteRuntime
        {
            Texture = GumTextures.Load(KnobPath),
            TextureAddress = TextureAddress.Custom,
            TextureLeft = KnobSource.X,
            TextureTop = KnobSource.Y,
            TextureWidth = KnobSource.Width,
            TextureHeight = KnobSource.Height,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = KnobSize,
            Height = KnobSize,
            YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center,
        };
        visual.Children.Insert(0, _track);
        visual.Children.Insert(1, _knob);

        ToggleButtonVisual.ToggleButtonCategoryStates states = visual.States;
        SetState(states.EnabledOn, enabled: true, hovered: false, pushed: false);
        SetState(states.EnabledOff, enabled: true, hovered: false, pushed: false);
        SetState(states.FocusedOn, enabled: true, hovered: true, pushed: false);
        SetState(states.FocusedOff, enabled: true, hovered: true, pushed: false);
        SetState(states.HighlightedOn, enabled: true, hovered: true, pushed: false);
        SetState(states.HighlightedOff, enabled: true, hovered: true, pushed: false);
        SetState(states.HighlightedFocusedOn, enabled: true, hovered: true, pushed: false);
        SetState(states.HighlightedFocusedOff, enabled: true, hovered: true, pushed: false);
        SetState(states.PushedOn, enabled: true, hovered: true, pushed: true);
        SetState(states.PushedOff, enabled: true, hovered: true, pushed: true);
        SetState(states.DisabledOn, enabled: false, hovered: false, pushed: false);
        SetState(states.DisabledOff, enabled: false, hovered: false, pushed: false);
        SetState(states.DisabledFocusedOn, enabled: false, hovered: false, pushed: false);
        SetState(states.DisabledFocusedOff, enabled: false, hovered: false, pushed: false);

        Checked += (_, _) => Slide();
        Unchecked += (_, _) => Slide();
        _knobAt = Target;
        UpdateState();
        Refresh();
    }

    // 0 at the left (off), 1 at the right (on).
    private float Target => IsChecked == true ? 1 : 0;

    private void SetState(StateSave state, bool enabled, bool hovered, bool pushed)
    {
        state.Clear();
        state.Apply = () =>
        {
            ((ToggleButtonVisual)Visual).FocusedIndicator.Visible = false;
            _enabled = enabled;
            _hovered = hovered;
            _pushed = pushed;
            Refresh();
        };
    }

    private void Slide() => GumHost.Animate(this, seconds =>
    {
        float target = Target;
        _knobAt += (target - _knobAt) * MathF.Min(1, SlideSpeed * seconds);
        if (MathF.Abs(target - _knobAt) < 0.01f)
            _knobAt = target;
        Refresh();
        return _knobAt != target;
    });

    private void Refresh()
    {
        bool on = IsChecked == true;
        _track.Texture = Track(on ? TrackOnPath : TrackOffPath);
        Color tint = !_enabled ? new Color(110, 110, 110) : _pushed ? new Color(200, 200, 200) : _hovered ? Color.White : new Color(225, 225, 225);
        _track.Color = tint;
        // A gold knob while hovered, as the game's hovered UI text.
        _knob.Color = !_enabled ? new Color(130, 130, 130) : _hovered ? new Color(255, 231, 69) : Color.White;
        // The knob sits inside the track's 1-pixel rim at either end.
        float inset = (Visual.Height - KnobSize) / 2;
        float travel = Visual.Width - KnobSize - 2 * inset;
        _knob.X = UiPixels.Snap(inset + travel * _knobAt);
    }
}
