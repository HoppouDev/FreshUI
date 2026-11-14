using Gum;

namespace FreshUI.GumUi;

/// <summary>
/// The UI's pixel grid. All UI is laid out in UI pixels, and Gum's camera draws each as
/// <see cref="Scale"/> screen pixels, a whole number, so pixel art stays even. One UI pixel is one
/// pixel of the UI art (2×2 texels of the slot, item and lock textures).
/// </summary>
static class UiPixels
{
    /// <summary>Screen pixels per UI pixel.</summary>
    public static int Scale { get; private set; } = 1;

    /// <summary>
    /// Draws each UI pixel as <paramref name="scale"/> screen pixels. Gum's canvas becomes the
    /// window's size in UI pixels, and follows the window as it resizes.
    /// </summary>
    public static void SetScale(int scale)
    {
        Scale = Math.Max(1, scale);
        GumService.Default.EnableExpandToWindow(Scale);
        // The canvas changed size: elements sized relative to it (screens filling it) follow now.
        GumService.Default.Root.UpdateLayout();
        GumText.RefreshPixelScale();
    }

    /// <summary>A length in UI pixels rounded to whole screen pixels.</summary>
    public static float Snap(float length) => MathF.Round(length * Scale) / Scale;
}
