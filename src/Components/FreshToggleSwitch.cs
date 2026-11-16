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
/// An on/off switch: a pill-shaped track (FreshUI's <c>toggle</c> art, blue off and teal on: <see cref="Art"/>) with a
/// knob (a white square in a black ring, its corners clear: <see cref="Knob"/>) that slides across when it flips. A Forms
/// <see cref="ToggleButton"/>, so <see cref="ToggleButton.IsChecked"/>, its events, keyboard focus and
/// remote automation work as for any toggle. Hovered or focused (by the gamepad), the knob is gold.
/// Sizes are in UI pixels; FreshUI's art is a texel a pixel.
/// </summary>
class FreshToggleSwitch : ToggleButton
{
    public static string TrackOffPath { get; set; } = "Images/UI/Scrollbar";
    public static string TrackOnPath { get; set; } = "Images/UI/Scrollbar2";

    // The toggle art's rounded ends, 3 pixels deep.
    private const int TrackCorner = 3;
    /// <summary>The knob's size in UI pixels (the switch's and the slider's: <see cref="Knob"/>).</summary>
    internal const float KnobSize = 7;

    /// <summary>
    /// The game's track art (16 texels tall, 8 pixels) with an interior pixel row repeated: 9 pixels (the progress
    /// bar's), drawn without stretching. (A nine-slice stretching the art's 2-pixel middle to 3 doubles one row
    /// unevenly, shifting the rounded ends.)
    /// </summary>
    internal static Texture2D Track(string path) => GumTextures.Taller(path, 6, 2);

    /// <summary>
    /// FreshUI's component art (<c>resources/textures/components/{name}.png</c>, a texel a UI pixel) nine-sliced to
    /// <paramref name="height"/> pixels (<paramref name="corner"/> at each edge kept), blue as drawn or, <paramref name="on"/>,
    /// teal: the game's on track's (<see cref="TrackOnPath"/>) rim and fill colours in place of the art's own.
    /// </summary>
    internal static Texture2D Art(string name, int height, int corner, bool on = false) =>
        GumTextures.Made($"fresh-ui:{name}#{height}{(on ? "-on" : "")}", () =>
        {
            Texture2D source = GumTextures.Resource($"textures/components/{name}.png");
            Texture2D art = GumTextures.Sliced(source, source.Width, height, corner);
            if (on)
                Recolor(art, GumTextures.Load(TrackOnPath));
            return art;
        });

    // The art's rim (its second row) and fill (its middle), read at its middle column, as the game's 2×2-texel art's.
    private static void Recolor(Texture2D art, Texture2D game)
    {
        var texels = new Color[art.Width * art.Height];
        art.GetData(texels);
        var gameTexels = new Color[game.Width * game.Height];
        game.GetData(gameTexels);
        Color rim = texels[art.Width + art.Width / 2], fill = texels[art.Height / 2 * art.Width + art.Width / 2];
        Color onRim = gameTexels[2 * game.Width + game.Width / 2], onFill = gameTexels[game.Height / 2 * game.Width + game.Width / 2];
        for (int i = 0; i < texels.Length; i++)
            texels[i] = texels[i] == rim ? onRim : texels[i] == fill ? onFill : texels[i];
        art.SetData(texels);
    }

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
        // The 7-pixel knob 2 whole pixels inside the track all round (the rim, and a pixel of padding).
        visual.Width = 20;
        visual.Height = 11;

        _track = new FreshPill(TrackCorner, 1)
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
        };
        _knob = new SpriteRuntime
        {
            Texture = Knob(),
            TextureAddress = TextureAddress.EntireTexture,
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

    /// <summary>
    /// The switch's and slider's knob, 7 pixels square: FreshUI's <c>knob</c> art (a black ring round white, its corners
    /// clear) nine-sliced to that size.
    /// </summary>
    internal static Texture2D Knob() => GumTextures.Made("fresh-ui:knob", () =>
        GumTextures.Sliced(GumTextures.Resource("textures/components/knob.png"), (int)KnobSize, (int)KnobSize, 1));

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
        _track.Texture = Art("toggle", (int)Visual.Height, TrackCorner, on);
        Color tint = !_enabled ? new Color(110, 110, 110) : _pushed ? new Color(200, 200, 200) : _hovered ? Color.White : new Color(225, 225, 225);
        _track.Color = tint;
        // A gold knob while hovered, as the game's hovered UI text.
        _knob.Color = !_enabled ? new Color(130, 130, 130) : _hovered ? new Color(255, 231, 69) : Color.White;
        // The knob sits inside the track's rim and padding at either end.
        float inset = (Visual.Height - KnobSize) / 2;
        float travel = Visual.Width - KnobSize - 2 * inset;
        _knob.X = UiPixels.Snap(inset + travel * _knobAt);
    }
}
