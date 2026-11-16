using FreshUI.GumUi;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace FreshUI.Components;

/// <summary>
/// Text that follows the cursor while it's over an element (<see cref="Attach(InteractiveGue, Func{string?})"/>),
/// or sits under a control while it has the gamepad's focus (<see cref="Attach(FrameworkElement, Func{string?})"/>):
/// outlined distance field text (see GumText) with the hotbar's drop shadow, just below and right of the cursor,
/// kept inside the canvas, over the menus (the overlay's root, which FreshUI draws; Gum's popup root isn't).
/// One tooltip shows at a time. Sizes are in UI pixels (see UiPixels).
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
    // The text's container, clipping it: to the scroll area its element is in while under it (the gamepad's focus), so
    // it scrolls out with its element; otherwise the canvas.
    private static ContainerRuntime? _clip;
    private static InteractiveGue? _owner;
    // Whether it shows under its element (the gamepad's focus) rather than by the cursor.
    private static bool _below;

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

    /// <summary>
    /// As <see cref="Attach(InteractiveGue, Func{string?})"/> for the cursor, and while the control has the
    /// gamepad's focus, under it.
    /// </summary>
    public static void Attach(FrameworkElement control, Func<string?> text)
    {
        var element = (InteractiveGue)control.Visual;
        Attach(element, text);
        control.GotFocus += (_, _) => Show(element, text, below: true);
        control.LostFocus += (_, _) => Hide(element);
    }

    private static void Show(InteractiveGue owner, Func<string?> content) => Show(owner, content, below: false);

    private static void Show(InteractiveGue owner, Func<string?> content, bool below)
    {
        _owner = owner;
        _below = below;
        // Made once (again after a hot reload added the clip: the old text, on the overlay's root, hidden).
        if (_clip == null && _text != null)
            _text.Visible = false;
        TextRuntime text = _clip == null ? _text = Create() : _text!;
        string? value = content();
        text.Visible = !string.IsNullOrEmpty(value);
        if (!text.Visible)
        {
            return;
        }

        text.Text = value!;
        // Over everything else on the overlay, screens added since included.
        GraphicalUiElement parent = GumHost.OverlayRoot;
        if (parent.Children[^1] != _clip)
        {
            parent.Children.Remove(_clip);
            parent.Children.Add(_clip);
        }
        Place(owner, text, below);
    }

    private static void Place(InteractiveGue owner, TextRuntime text, bool below)
    {
        ICursor cursor = FrameworkElement.MainCursor;
        float x = below ? owner.AbsoluteLeft : cursor.XRespectingGumZoomAndBounds() + CursorOffset;
        float y = below ? owner.AbsoluteTop + owner.AbsoluteHeight + 1 : cursor.YRespectingGumZoomAndBounds() + CursorOffset;
        // Kept inside the canvas, moved back left or up from the edge it would cross.
        x = MathF.Min(x, GumService.Default.CanvasWidth - text.AbsoluteWidth - EdgeMargin);
        y = MathF.Min(y, GumService.Default.CanvasHeight - text.AbsoluteHeight - EdgeMargin);
        float left = 0, top = 0, right = GumService.Default.CanvasWidth, bottom = GumService.Default.CanvasHeight;
        for (var parent = below ? owner.Parent as GraphicalUiElement : null; parent != null; parent = parent.Parent as GraphicalUiElement)
        {
            if (!parent.ClipsChildren)
                continue;
            (left, top) = (MathF.Max(left, parent.AbsoluteLeft), MathF.Max(top, parent.AbsoluteTop));
            (right, bottom) = (MathF.Min(right, parent.AbsoluteLeft + parent.GetAbsoluteWidth()), MathF.Min(bottom, parent.AbsoluteTop + parent.GetAbsoluteHeight()));
        }
        ContainerRuntime clip = _clip!;
        (clip.X, clip.Y, clip.Width, clip.Height) = (left, top, MathF.Max(0, right - left), MathF.Max(0, bottom - top));
        // Snapped to screen pixels, not UI pixels, so it follows the cursor smoothly at any UI scale.
        text.X = UiPixels.Snap(MathF.Max(EdgeMargin, x)) - left;
        text.Y = UiPixels.Snap(MathF.Max(EdgeMargin, y)) - top;
    }

    /// <summary>
    /// After each Gum update: the tooltip goes with its element when that's no longer shown (hidden, or removed, as
    /// a screen closing or a list rebuilt), which no roll-off reports.
    /// </summary>
    internal static void Update()
    {
        if (_owner != null && !GumHost.IsShown(_owner))
            Hide(_owner);
        // Under its focused element, following it as it moves (a list scrolled by the right stick, not the cursor).
        else if (_owner != null && _below && _text is { Visible: true })
            Place(_owner, _text, below: true);
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
        text.FontSize = FontSize;
        text.Color = Color;
        GumText.SetOutline(text, OutlineWidth, Microsoft.Xna.Framework.Color.Black);
        GumText.SetDropShadow(text, 1);
        _clip = new ContainerRuntime
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            ClipsChildren = true,
            HasEvents = false,
        };
        _clip.Children.Add(text);
        GumHost.OverlayRoot.Children.Add(_clip);
        return text;
    }
}
