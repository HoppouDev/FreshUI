using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace FreshUI.Components;

/// <summary>
/// A scrolling area with the game's scrollbar: its pill-shaped track and white handle, no arrow
/// buttons. The mouse wheel scrolls smoothly by default (<see cref="SmoothScrolling"/>): each notch
/// sets a glide going (see <see cref="MomentumScroll"/>), a fast spin goes further, and dragging the
/// handle stops it. A Forms <see cref="ScrollViewer"/>: add content with AddChild or to InnerPanel.
/// Sizes are in UI pixels; the scrollbar art is 2×2 texels a pixel.
/// </summary>
class FreshScrollViewer : ScrollViewer
{
    public static string TrackPath { get; set; } = "Images/UI/Scrollbar";
    public static string ThumbPath { get; set; } = "Images/UI/ScrollbarInner";

    private const float Texel = 0.5f;
    // The track and handle art are 20 texels across with ends rounded over 6.
    private const int Corner = 6;
    private const float BarWidth = 20 * Texel;
    // The handle runs this far inside the track's ends.
    private const float ThumbInset = 3;

    private readonly MomentumScroll.Glide _glide = new();

    // The handle at rest, a little dim, as the game's.
    private static readonly Color ThumbColor = new(217, 217, 217);

    /// <summary>Whether the wheel glides (true) or jumps a notch at a time (false).</summary>
    public bool SmoothScrolling { get; set; } = true;

    /// <summary>UI pixels a notch of the wheel scrolls.</summary>
    public float WheelStep { get; set; } = 16;


    public FreshScrollViewer()
    {
        var visual = (ScrollViewerVisual)Visual;
        visual.Background.Visible = false;
        visual.FocusedIndicator.Visible = false;
        // Gum's own wheel handling, off: this control's moves the bar (see OnWheel).
        MouseWheelScrollSpeed = 0;
        Visual.MouseWheelScroll += OnWheel;

        var bar = (ScrollBarVisual)visual.VerticalScrollBarInstance;
        // The orientation state lays the bar out again whenever it's applied; restyle after it.
        Action? vertical = bar.States.OrientationStates.Vertical.Apply;
        bar.States.OrientationStates.Vertical.Apply = () =>
        {
            vertical?.Invoke();
            Restyle(bar);
        };
        Restyle(bar);
        bar.ThumbInstance.Push += (_, _) => _glide.Stop();
        GotFocus += (_, _) => bar.ThumbInstance.BackgroundColor = FreshButton.FocusColor;
        LostFocus += (_, _) => bar.ThumbInstance.BackgroundColor = ThumbColor;
    }

    private static void Restyle(ScrollBarVisual bar)
    {
        bar.Width = BarWidth;
        if (bar.UpButtonInstance != null)
            bar.UpButtonInstance.Visible = false;
        if (bar.DownButtonInstance != null)
            bar.DownButtonInstance.Visible = false;

        bar.ThumbContainer.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        bar.ThumbContainer.YOrigin = VerticalAlignment.Center;
        bar.ThumbContainer.Y = 0;
        bar.ThumbContainer.Height = -2 * ThumbInset;
        bar.ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;

        bar.TrackInstance.Texture = GumTextures.Load(TrackPath);
        bar.TrackInstance.TextureAddress = TextureAddress.EntireTexture;
        bar.TrackInstance.CustomFrameTextureCoordinateWidth = Corner;
        bar.TrackInstance.BorderScale = Texel;
        bar.TrackInstance.Color = Color.White;
        bar.TrackInstance.Y = 0;
        bar.TrackInstance.Height = 0;
        bar.TrackInstance.HeightUnits = DimensionUnitType.RelativeToParent;

        ButtonVisual thumb = bar.ThumbInstance;
        thumb.Background.Texture = GumTextures.Load(ThumbPath);
        thumb.Background.TextureAddress = TextureAddress.EntireTexture;
        thumb.Background.CustomFrameTextureCoordinateWidth = Corner;
        thumb.Background.BorderScale = Texel;
        thumb.FocusedIndicator.Visible = false;
        if (thumb.TextInstance != null)
            thumb.TextInstance.Visible = false;
        // As the game's handle: a little dim at rest; Gum's button states lighten it while hovered.
        thumb.BackgroundColor = ThumbColor;
    }

    private void OnWheel(object? sender, RoutedEventArgs args)
    {
        ScrollBar? bar = VerticalScrollBar;
        if (bar == null || bar.Maximum <= bar.Minimum)
            return;
        float step = -FrameworkElement.MainCursor.ZVelocity * WheelStep;
        // At the end it's turned towards, the wheel goes to whatever is behind.
        if (step == 0 || step < 0 && bar.Value <= bar.Minimum || step > 0 && bar.Value >= bar.Maximum)
            return;
        args.Handled = true;
        if (!SmoothScrolling)
        {
            VerticalScrollBarValue += step;
            return;
        }
        _glide.Push(step);
        GumHost.Animate(this, _ =>
        {
            float distance = _glide.Advance();
            if (distance != 0)
            {
                double before = VerticalScrollBarValue;
                VerticalScrollBarValue = before + distance;
                _glide.Moved(distance, (float)(VerticalScrollBarValue - before));
            }
            return _glide.IsMoving;
        });
    }
}
