using FreshUI.GumUi;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;

namespace FreshUI.Components;

/// <summary>A Forms label drawn from the text font's distance field (see GumText), sharp at any font size.</summary>
class FreshLabel : Label
{
    private TextRuntime TextVisual => (TextRuntime)Visual;

    /// <summary>The text's size in UI pixels per em (at least 1). Fractional sizes scale smoothly.</summary>
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

    /// <summary>Whether the text is slanted (sheared procedurally, see GumText.ItalicSlant).</summary>
    public bool IsItalic
    {
        get => TextVisual.IsItalic;
        set => TextVisual.IsItalic = value;
    }

    /// <summary>The fill colour, multiplying the gradient if there is one.</summary>
    public Color Color
    {
        get => TextVisual.Color;
        set => TextVisual.Color = value;
    }

    /// <summary>The outline's width in UI pixels (0 for none), up to a quarter of the font size.</summary>
    public float OutlineWidth
    {
        get => GumText.GetOutline(TextVisual).Width;
        set => GumText.SetOutline(TextVisual, value, OutlineColor);
    }

    public Color OutlineColor
    {
        get => GumText.GetOutline(TextVisual).Color;
        set => GumText.SetOutline(TextVisual, OutlineWidth, value);
    }

    /// <summary>A vertical gradient from the capitals' top to the baseline, or null for none.</summary>
    public (Color Top, Color Bottom)? Gradient
    {
        get => GumText.GetGradient(TextVisual);
        set => GumText.SetGradient(TextVisual, value);
    }

    public bool HasShadow
    {
        get => TextVisual.HasDropshadow;
        set => TextVisual.HasDropshadow = value;
    }

    public Color ShadowColor
    {
        get => TextVisual.DropshadowColor;
        set => TextVisual.DropshadowColor = value;
    }

    /// <summary>The shadow's offset in UI pixels at the text's size.</summary>
    public Vector2 ShadowOffset
    {
        get => new(TextVisual.DropshadowOffsetX, TextVisual.DropshadowOffsetY);
        set
        {
            TextVisual.DropshadowOffsetX = value.X;
            TextVisual.DropshadowOffsetY = value.Y;
        }
    }

    /// <summary>The shadow's blur radius in UI pixels, up to an eighth of the font size.</summary>
    public float ShadowBlur
    {
        get => TextVisual.DropshadowBlur;
        set => TextVisual.DropshadowBlur = value;
    }
}
