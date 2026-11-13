using FreshUI.GumUi;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace FreshUI.Components;

/// <summary>
/// Text that follows the cursor while it's over an element (<see cref="Attach"/>): bordered distance
/// field text (see GumText) just below and right of the cursor, kept inside the canvas, above all other
/// UI. One tooltip shows at a time. Sizes are in UI pixels (see UiPixels).
/// </summary>
static class FreshTooltip
{
    /// <summary>The text's size, in UI pixels per em.</summary>
    public static float FontSize { get; set; } = 9.5f;

    /// <summary>The text's border width, in UI pixels.</summary>
    public static float OutlineWidth { get; set; } = 0.5f;

    public static Color Color { get; set; } = Color.White;

    /// <summary>How far right of and below the cursor the text starts, in UI pixels.</summary>
    public static float CursorOffset { get; set; } = 7;

    /// <summary>The space the text keeps from the canvas's edges, in UI pixels.</summary>
    public static float EdgeMargin { get; set; } = 2;

    private static TextRuntime? _text;
    private static InteractiveGue? _owner;

    /// <summary>
    /// Shows <paramref name="text"/>'s result while the cursor is over <paramref name="element"/>, read
    /// again when the cursor moves or the element is clicked (so it can describe a state the click
    /// changed). A null or empty result hides the tooltip.
    /// </summary>
    public static void Attach(InteractiveGue element, Func<string?> text)
    {
        element.RollOn += (_, _) => Show(element, text);
        element.RollOver += (_, _) => Show(element, text);
        // After the element's own click handlers, which were attached when it was made.
        element.Click += (_, _) => Show(element, text);
        element.RollOff += (_, _) => Hide(element);
    }

    private static void Show(InteractiveGue owner, Func<string?> content)
    {
        _owner = owner;
        TextRuntime text = _text ??= Create();
        string? value = content();
        text.Visible = !string.IsNullOrEmpty(value);
        if (!text.Visible)
        {
            return;
        }

        text.Text = value!;
        text.FontSize = FontSize;
        text.Color = Color;
        GumText.SetOutline(text, OutlineWidth, Microsoft.Xna.Framework.Color.Black);
        text.HasDropshadow = true;
        text.DropshadowColor = Color.Black;
        text.DropshadowBlur = 1;
        text.DropshadowOffsetX = 0;
        text.DropshadowOffsetY = 1;
        text.DropshadowAlpha = 100;

        ICursor cursor = FrameworkElement.MainCursor;
        float x = cursor.XRespectingGumZoomAndBounds() + CursorOffset;
        float y = cursor.YRespectingGumZoomAndBounds() + CursorOffset;
        // Kept inside the canvas, moved back left or up from the edge it would cross.
        x = MathF.Min(x, GumService.Default.CanvasWidth - text.AbsoluteWidth - EdgeMargin);
        y = MathF.Min(y, GumService.Default.CanvasHeight - text.AbsoluteHeight - EdgeMargin);
        // Snapped to screen pixels, not UI pixels, so it follows the cursor smoothly at any UI scale.
        text.X = UiPixels.Snap(MathF.Max(EdgeMargin, x));
        text.Y = UiPixels.Snap(MathF.Max(EdgeMargin, y));
    }

    private static void Hide(InteractiveGue owner)
    {
        if (_owner == owner && _text != null)
        {
            _text.Visible = false;
            _owner = null;
        }
    }

    private static TextRuntime Create()
    {
        var text = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            Visible = false,
        };
        // Never under the cursor as far as input goes, or it would take the hover from its element.
        text.HasEvents = false;
        GumService.Default.PopupRoot.Children.Add(text);
        return text;
    }
}
