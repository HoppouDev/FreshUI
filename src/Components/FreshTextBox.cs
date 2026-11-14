using FreshUI.GumUi;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;

namespace FreshUI.Components;

/// <summary>
/// A one-line text field, as the world creation's Name and Seed fields: a <see cref="FreshPanelButton"/>'s
/// panel with an optional <see cref="Label"/> ("Name:") before the text, and the placeholder muted while
/// it's empty; lit with a gold border while hovered or being typed in. A Forms <see cref="TextBox"/>:
/// typing, the caret, selection, the clipboard, focus and automation (<c>set_text</c>) work as usual.
/// </summary>
class FreshTextBox : TextBox
{
    private const float Indent = 5;

    private readonly NineSliceRuntime _border;
    private readonly TextRuntime _label;
    private bool _lit, _disabled;

    public FreshTextBox()
    {
        var visual = (TextBoxVisual)Visual;
        NineSliceRuntime background = visual.Background;
        NineSliceRuntime panel = FreshPanel.Slice(FreshStyle.PanelBackground, FreshStyle.Button);
        background.Texture = panel.Texture;
        background.TextureAddress = panel.TextureAddress;
        background.CustomFrameTextureCoordinateWidth = panel.CustomFrameTextureCoordinateWidth;
        background.BorderScale = panel.BorderScale;
        _border = FreshPanel.Slice(FreshStyle.PanelBorder, Color.Black);
        visual.Children.Insert(visual.Children.IndexOf(background) + 1, _border);
        visual.FocusedIndicator.Visible = false;
        visual.Height = 16;

        _label = new TextRuntime { X = Indent, FontSize = 9, Text = "", Color = Color.White };
        _label.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        _label.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        GumText.SetOutline(_label, 0.75f, Color.Black);
        visual.Children.Add(_label);
        foreach (TextRuntime text in new[] { visual.TextInstance, visual.PlaceholderTextInstance })
        {
            text.FontSize = 9;
            GumText.SetOutline(text, 0.75f, Color.Black);
        }
        visual.TextInstance.Color = Color.White;
        visual.CaretInstance.Color = Color.White;
        visual.PlaceholderTextInstance.Color = FreshStyle.MutedText * 0.8f;

        foreach (StateSave state in new[] { visual.States.Enabled, visual.States.Disabled, visual.States.Highlighted, visual.States.Focused })
        {
            bool lit = state == visual.States.Highlighted || state == visual.States.Focused;
            bool disabled = state == visual.States.Disabled;
            state.Clear();
            state.Apply = () =>
            {
                visual.FocusedIndicator.Visible = false;
                if (lit && !_lit && !disabled)
                    SoundEngine.PlaySound(SoundID.MenuTick);
                _lit = lit;
                _disabled = disabled;
                Refresh();
            };
        }
        Label = "";
        UpdateState();
    }

    /// <summary>The field's name before its text ("Name:"), or empty for none.</summary>
    public string Label
    {
        get => _label.Text ?? "";
        set
        {
            _label.Text = value;
            _label.Visible = value.Length > 0;
            // The text starts after the label (and a space's width).
            ((TextBoxVisual)Visual).ClipContainer.X = Indent + (value.Length > 0 ? MathF.Ceiling(_label.AbsoluteWidth) + 3 : 0);
        }
    }

    private void Refresh()
    {
        var visual = (TextBoxVisual)Visual;
        visual.Background.Color = _disabled ? FreshStyle.Button * 0.6f : _lit ? FreshStyle.ButtonLit : FreshStyle.Button;
        _border.Color = _lit && !_disabled ? FreshStyle.Gold : Color.Black;
        _label.Color = _disabled ? FreshStyle.Disabled : Color.White;
    }
}
