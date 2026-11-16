using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace FreshUI.Components;

/// <summary>
/// A setting's value as text that acts when clicked (a choice that cycles, a key to rebind, a page to
/// open): no panel, its text right-aligned in <see cref="FreshStyle.MutedText"/>, white and then gold
/// while hovered, pushed or focused, as the game's hovered UI text. A Forms <see cref="Button"/>, so it
/// takes the mouse, the gamepad (A) and automation as any button. Sized to its text.
/// </summary>
public class FreshValueButton : Button
{
    private bool _lit, _disabled;
    private Color _color = FreshStyle.MutedText;

    public FreshValueButton()
    {
        var visual = (ButtonVisual)Visual;
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;
        visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        visual.Width = 0;
        visual.HeightUnits = DimensionUnitType.Absolute;
        visual.Height = 14;
        var text = visual.TextInstance;
        text.FontSize = 9;
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.HorizontalAlignment = HorizontalAlignment.Right;
        GumText.SetOutline(text, 0.75f, Color.Black);
        foreach (StateSave state in new[] { visual.States.Enabled, visual.States.Focused, visual.States.Highlighted,
            visual.States.HighlightedFocused, visual.States.Pushed, visual.States.Disabled, visual.States.DisabledFocused })
        {
            bool lit = state != visual.States.Enabled && state != visual.States.Disabled && state != visual.States.DisabledFocused;
            bool disabled = state == visual.States.Disabled || state == visual.States.DisabledFocused;
            state.Clear();
            state.Apply = () =>
            {
                visual.FocusedIndicator.Visible = false;
                _lit = lit;
                _disabled = disabled;
                Refresh();
            };
        }
        UpdateState();
    }

    /// <summary>The text's colour at rest (lit, it's gold).</summary>
    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value)
                return;
            _color = value;
            Refresh();
        }
    }

    /// <summary>Lit as if hovered (its row is, or it's waiting for input, as a key being rebound).</summary>
    public bool IsLit
    {
        get => _forced;
        set
        {
            if (_forced == value)
                return;
            _forced = value;
            Refresh();
        }
    }
    private bool _forced;

    private void Refresh()
    {
        Color color = _disabled ? FreshStyle.Disabled : _lit || _forced ? FreshStyle.Gold : _color;
        var text = ((ButtonVisual)Visual).TextInstance;
        if (text.Color != color)
            text.Color = color;
    }
}
