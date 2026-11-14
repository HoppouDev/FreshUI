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
/// A button in the game's panel style, as FreshUI's menus (the pause menu, the settings window's actions
/// and tabs, the Mods window's footer) draw them: a panel in <see cref="FreshStyle.Button"/> with its text
/// centred, lit (<see cref="FreshStyle.ButtonLit"/>, a gold border) while hovered, focused or
/// <see cref="IsChosen"/> (an open tab), with the menus' tick as the mouse or the gamepad's focus comes
/// onto it. A Forms <see cref="Button"/>: Click, focus, gamepad (A) and automation as usual.
/// </summary>
class FreshPanelButton : Button
{
    private readonly NineSliceRuntime _border;
    private bool _chosen, _lit, _disabled;

    public FreshPanelButton()
    {
        var visual = (ButtonVisual)Visual;
        // Replace Gum's backing with the panel's: its background, and its border over it.
        NineSliceRuntime background = visual.Background;
        NineSliceRuntime panel = FreshPanel.Slice(FreshStyle.PanelBackground, FreshStyle.Button);
        background.Texture = panel.Texture;
        background.TextureAddress = panel.TextureAddress;
        background.CustomFrameTextureCoordinateWidth = panel.CustomFrameTextureCoordinateWidth;
        background.BorderScale = panel.BorderScale;
        _border = FreshPanel.Slice(FreshStyle.PanelBorder, Color.Black);
        visual.Children.Insert(visual.Children.IndexOf(background) + 1, _border);
        visual.FocusedIndicator.Visible = false;
        GumText.SetOutline(visual.TextInstance, 0.75f, Color.Black);
        visual.TextInstance.FontSize = 12;

        foreach (StateSave state in new[] { visual.States.Enabled, visual.States.Focused, visual.States.Highlighted,
            visual.States.HighlightedFocused, visual.States.Pushed, visual.States.Disabled, visual.States.DisabledFocused })
        {
            bool lit = state != visual.States.Enabled && state != visual.States.Disabled && state != visual.States.DisabledFocused;
            bool disabled = state == visual.States.Disabled || state == visual.States.DisabledFocused;
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
        UpdateState();
    }

    /// <summary>The text's size in UI pixels per em.</summary>
    public float FontSize
    {
        get => ((ButtonVisual)Visual).TextInstance.FontSize;
        set => ((ButtonVisual)Visual).TextInstance.FontSize = MathF.Max(1, value);
    }

    /// <summary>Puts the text at the left, <paramref name="indent"/> UI pixels in (a list's rows, a window's tabs).</summary>
    public void AlignTextLeft(float indent)
    {
        TextRuntime text = ((ButtonVisual)Visual).TextInstance;
        text.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        text.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Left;
        text.X = indent;
        text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        text.Width = -2 * indent;
        text.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Left;
    }

    /// <summary>Lit as if hovered, while it stands for what's chosen (the open tab).</summary>
    public bool IsChosen
    {
        get => _chosen;
        set { _chosen = value; Refresh(); }
    }

    private void Refresh()
    {
        var visual = (ButtonVisual)Visual;
        bool lit = (_lit || _chosen) && !_disabled;
        visual.Background.Color = lit ? FreshStyle.ButtonLit : _disabled ? FreshStyle.Button * 0.6f : FreshStyle.Button;
        _border.Color = lit ? FreshStyle.Gold : Color.Black;
        visual.TextInstance.Color = _disabled ? FreshStyle.Disabled : Color.White;
    }
}
