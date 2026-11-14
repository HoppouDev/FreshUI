using FreshUI.GumUi;
using Gum;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Managers;

namespace FreshUI.Components;

/// <summary>
/// A Forms button with a nine-sliced backing texture and distance field text (see GumText) with a
/// black border, as <see cref="FreshLabel"/> text can have. Focused (by the gamepad), its text is gold,
/// as the game's menus mark the selected item.
/// </summary>
class FreshButton : Button
{
    /// <summary>The backing texture every button uses, relative to the executable. Set it before creating buttons.</summary>
    public static string BackingPath { get; set; } = "";

    // The text's border in UI pixels. It stays this wide at any font size, the usual weight for UI
    // text with a border.
    private const float OutlineWidth = 0.75f;

    private TextRuntime TextVisual => ((ButtonVisual)Visual).TextInstance;

    /// <summary>The gold of the game's hovered and selected UI text.</summary>
    public static readonly Color FocusColor = new(255, 231, 69);

    public FreshButton()
    {
        var visual = (ButtonVisual)Visual;

        // The texture is already coloured, so states tint it (multiply) rather than recolor it.
        visual.Background.Texture = GumTextures.Load(BackingPath);
        visual.Background.TextureAddress = TextureAddress.EntireTexture;
        // The backing's art has a texel per pixel: one UI pixel each, on the UI's grid.
        visual.Background.BorderScale = 1;

        var normal = new Color(225, 225, 225);
        var pushed = new Color(170, 170, 170);
        var disabled = new Color(110, 110, 110);
        var disabledText = new Color(130, 130, 130);

        SetState(visual, visual.States.Enabled, normal, Color.White, focused: false);
        SetState(visual, visual.States.Focused, Color.White, FocusColor, focused: true);
        SetState(visual, visual.States.Highlighted, Color.White, Color.White, focused: false);
        SetState(visual, visual.States.HighlightedFocused, Color.White, FocusColor, focused: true);
        SetState(visual, visual.States.Pushed, pushed, Color.White, focused: false);
        SetState(visual, visual.States.Disabled, disabled, disabledText, focused: false);
        SetState(visual, visual.States.DisabledFocused, disabled, disabledText, focused: true);

        GumText.SetOutline(TextVisual, OutlineWidth, Color.Black);
        UpdateState();
    }

    /// <summary>The text's size in UI pixels per em (at least 1). Its border keeps its width.</summary>
    public float FontSize
    {
        get => TextVisual.FontSize;
        set => TextVisual.FontSize = MathF.Max(1, value);
    }

    /// <summary>The text's CSS-style weight, 1 to 1000 (GumText.DesignWeight draws the font as designed).</summary>
    public float FontWeight
    {
        get => GumText.GetFontWeight(TextVisual);
        set => GumText.SetFontWeight(TextVisual, value);
    }

    private static void SetState(ButtonVisual visual, StateSave state, Color background, Color text, bool focused)
    {
        state.Clear();
        state.Apply = () =>
        {
            visual.Background.Color = background;
            visual.TextInstance.Color = text;
            // Focus shows in the text's colour, not Gum's indicator bar.
            visual.FocusedIndicator.Visible = false;
        };
    }
}
