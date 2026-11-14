using FreshUI.GumUi;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace FreshUI.Components;

/// <summary>
/// A horizontal slider in the same art as <see cref="FreshToggleSwitch"/>: the scrollbar's pill as its
/// track, filled in teal up to the value, and the settings toggle's round knob, gold while hovered,
/// dragged or focused. A Forms <see cref="Slider"/>, so Minimum, Maximum, Value, snapping
/// (IsSnapToTickEnabled with TicksFrequency) and its events work as usual. Focused by the gamepad, the
/// D-pad (or left stick) steps it by 1% of its range and the bumpers by 10%, or by whole ticks when it
/// snaps. The art is 2×2 texels a UI
/// pixel, so it stays even at every UI scale.
/// </summary>
class FreshSlider : Slider
{
    private const float Texel = 0.5f;
    // As FreshToggleSwitch: a track 9 pixels tall with ends rounded over 6 texels, and the 7-pixel knob
    // a pixel inside it all round.
    private const int TrackCorner = 6;
    private const float TrackHeight = 9;
    private const float Inset = 1;
    private static readonly float KnobSize = FreshToggleSwitch.KnobSource.Width * Texel;
    // Where the knob's travel (its middle) starts: the inset and half the knob in from the left.
    private static readonly float TravelStart = Inset + KnobSize / 2;

    private static readonly Color Gold = new(255, 231, 69);

    private readonly FreshPill _track;
    private readonly FreshPill _fill;
    private readonly SpriteRuntime _knob;
    private SpriteRuntime? _ramp;
    private Func<float, Color>? _rampColor;
    private Color[]? _rampColors;
    private int _hovered;
    private bool _pushed, _disabled;

    public FreshSlider()
    {
        var visual = (SliderVisual)Visual;
        visual.Width = 89;
        visual.Height = TrackHeight;
        visual.FocusedIndicator.Visible = false;

        // The knob travels the track's length less its own width, so it stays within the pill; the
        // travel and the track start on whole pixels from the left.
        visual.TrackInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        visual.TrackInstance.XOrigin = HorizontalAlignment.Left;
        visual.TrackInstance.X = TravelStart;
        visual.TrackInstance.Width = -2 * TravelStart;
        // The track (Gum's own nine-slice hidden: see FreshPill) out to the travel's ends under the
        // knob's halves, and the value, filled in from its left end to the knob's middle.
        visual.TrackBackground.Visible = false;
        _track = Pill(FreshToggleSwitch.TrackOffPath);
        _track.WidthUnits = DimensionUnitType.RelativeToParent;
        _track.Width = 2 * TravelStart;
        _fill = Pill(FreshToggleSwitch.TrackOnPath);
        int at = visual.TrackInstance.Children.IndexOf(visual.TrackBackground) + 1;
        visual.TrackInstance.Children.Insert(at, _track);
        visual.TrackInstance.Children.Insert(at + 1, _fill);

        // The Gum thumb keeps its behaviour (dragging, its hit area) with the knob as its only art.
        ButtonVisual thumb = visual.ThumbInstance!;
        thumb.Width = KnobSize;
        thumb.HeightUnits = DimensionUnitType.Absolute;
        thumb.Height = KnobSize;
        thumb.Background.Visible = false;
        thumb.FocusedIndicator.Visible = false;
        _knob = new SpriteRuntime
        {
            Texture = GumTextures.Load(FreshToggleSwitch.KnobPath),
            TextureAddress = TextureAddress.Custom,
            TextureLeft = FreshToggleSwitch.KnobSource.X,
            TextureTop = FreshToggleSwitch.KnobSource.Y,
            TextureWidth = FreshToggleSwitch.KnobSource.Width,
            TextureHeight = FreshToggleSwitch.KnobSource.Height,
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
        };
        thumb.Children.Add(_knob);
        // The thumb's own states would show its (hidden) backing; the slider's states colour the knob.
        foreach (StateSave state in new[] { thumb.States.Enabled, thumb.States.Focused, thumb.States.Highlighted,
            thumb.States.HighlightedFocused, thumb.States.Pushed, thumb.States.Disabled, thumb.States.DisabledFocused })
        {
            state.Clear();
            state.Apply = () => thumb.FocusedIndicator.Visible = false;
        }

        SliderVisual.SliderCategoryStates states = visual.States;
        SetState(states.Enabled, disabled: false);
        SetState(states.Focused, disabled: false);
        SetState(states.Highlighted, disabled: false);
        SetState(states.HighlightedFocused, disabled: false);
        SetState(states.Pushed, disabled: false);
        SetState(states.Disabled, disabled: true);
        SetState(states.DisabledFocused, disabled: true);

        // Hovered while the cursor is over the slider, its track or its knob (the topmost under the cursor
        // gets the events, so moving between them rolls off one and onto the next), pushed while one is
        // held: Gum's Slider doesn't update its state for these.
        foreach (Gum.Wireframe.InteractiveGue part in new Gum.Wireframe.InteractiveGue[] { visual, visual.TrackInstance, thumb })
        {
            part.RollOn += (_, _) => { _hovered++; Refresh(); };
            part.RollOff += (_, _) => { _hovered = Math.Max(0, _hovered - 1); Refresh(); };
            part.Push += (_, _) => { _pushed = true; Refresh(); };
            part.RemovedAsPushed += (_, _) => { _pushed = false; Refresh(); };
        }
        GotFocus += (_, _) =>
        {
            SetGamepadSteps();
            Refresh();
            GumHost.Animate(this, _ => StepCoarse());
        };
        LostFocus += (_, _) => Refresh();
        ValueChanged += (_, _) => Refresh();
        visual.TrackInstance.SizeChanged += (_, _) => Refresh();
        UpdateState();
        Refresh();
    }

    // Gum's D-pad handling steps by SmallChange; the bumpers (StepCoarse) by LargeChange.
    private void SetGamepadSteps()
    {
        double range = Maximum - Minimum;
        if (IsSnapToTickEnabled && TicksFrequency > 0)
        {
            SmallChange = TicksFrequency;
            LargeChange = Math.Max(TicksFrequency, Math.Round(range / 10 / TicksFrequency) * TicksFrequency);
        }
        else
        {
            SmallChange = range / 100;
            LargeChange = range / 10;
        }
    }

    private double _coarseAt = -1;

    // Once per Gum update (animations run every frame) while focused: a bumper, held, repeats.
    private bool StepCoarse()
    {
        if (!IsFocused)
            return false;
        double now = Gum.Wireframe.InteractiveGue.CurrentGameTime;
        if (now == _coarseAt)
            return true;
        _coarseAt = now;
        foreach (Gum.Input.IGamePad gamepad in GamePadsForUiControl)
        {
            int direction = gamepad.ButtonRepeatRate(Gum.Input.GamepadButton.LeftShoulder) ? -1
                : gamepad.ButtonRepeatRate(Gum.Input.GamepadButton.RightShoulder) ? 1 : 0;
            if (direction != 0 && IsEnabled)
            {
                double before = Value;
                Value = Math.Clamp(Value + direction * LargeChange, Minimum, Maximum);
                if (Value != before)
                    RaiseValueChangedByUi();
            }
        }
        return true;
    }

    // The ramp's texture is this many texels across, one a step.
    private const int RampSteps = 64;

    /// <summary>
    /// A colour bar (as the cursor colour settings): the track shows this function's colour at each value
    /// (0 to 1) in place of the teal fill, under the knob. Call <see cref="UpdateRamp"/> when what it
    /// depends on changes (another bar of the same colour).
    /// </summary>
    public Func<float, Color>? Ramp
    {
        get => _rampColor;
        set
        {
            _rampColor = value;
            if (value != null && _ramp == null)
            {
                var visual = (SliderVisual)Visual;
                _ramp = new SpriteRuntime
                {
                    Texture = new Texture2D(Terraria.Main.instance.GraphicsDevice, RampSteps, 1),
                    TextureAddress = TextureAddress.EntireTexture,
                    WidthUnits = DimensionUnitType.RelativeToParent,
                    HeightUnits = DimensionUnitType.Absolute,
                    Width = 0,
                    // Inside the pill's rim, a pixel clear of it.
                    Height = TrackHeight - 4,
                    YUnits = GeneralUnitType.PixelsFromMiddle,
                    YOrigin = VerticalAlignment.Center,
                };
                visual.TrackInstance.Children.Insert(visual.TrackInstance.Children.IndexOf(_fill) + 1, _ramp);
            }
            if (_ramp != null)
                _ramp.Visible = value != null;
            _fill.Visible = value == null;
            UpdateRamp();
        }
    }

    /// <summary>Redraws the colour bar's ramp (<see cref="Ramp"/>) if its colours changed.</summary>
    public void UpdateRamp()
    {
        if (_rampColor == null || _ramp == null)
            return;
        _rampColors ??= new Color[RampSteps];
        bool changed = false;
        for (int i = 0; i < RampSteps; i++)
        {
            Color color = _rampColor(i / (float)(RampSteps - 1));
            changed |= color != _rampColors[i];
            _rampColors[i] = color;
        }
        if (changed)
            _ramp.Texture!.SetData(_rampColors);
    }

    // The scrollbar's pill, 9 pixels tall, from the left end of the travel (under the knob's half).
    private static FreshPill Pill(string texture) => new(TrackCorner)
    {
        Texture = FreshToggleSwitch.Track(texture),
        XUnits = GeneralUnitType.PixelsFromSmall,
        X = -TravelStart,
        YUnits = GeneralUnitType.PixelsFromMiddle,
        YOrigin = VerticalAlignment.Center,
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        Height = TrackHeight,
    };

    private void SetState(StateSave state, bool disabled)
    {
        state.Clear();
        state.Apply = () =>
        {
            ((SliderVisual)Visual).FocusedIndicator.Visible = false;
            _disabled = disabled;
            Refresh();
        };
    }

    private void Refresh()
    {
        var visual = (SliderVisual)Visual;
        double range = Maximum - Minimum;
        float share = range > 0 ? (float)((Value - Minimum) / range) : 0;
        float travel = visual.TrackInstance.AbsoluteWidth;
        // Gum centres the thumb at share × travel, between pixels; the knob inside it moves onto the
        // nearest whole screen pixel, so its art stays even.
        float left = TravelStart + share * travel - KnobSize / 2;
        _knob.X = UiPixels.Snap(left) - left;
        _fill.Width = UiPixels.Snap(TravelStart + share * travel);
        Color tint = _disabled ? new Color(110, 110, 110) : Color.White;
        _track.Color = tint;
        _fill.Color = tint;
        bool highlighted = _hovered > 0 || _pushed || IsFocused;
        _knob.Color = _disabled ? new Color(130, 130, 130) : highlighted ? Gold : Color.White;
    }
}
