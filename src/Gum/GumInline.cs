using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace FreshUI.GumUi;

/// <summary>
/// A Gum element drawn on the spot, within the game's own drawing: a window's chrome (a
/// <c>FreshPanel</c>) where the game drew its box, so the game's content on the box (text with chat
/// tags, portraits, its buttons and their input) stays the game's and draws over it. The element has a
/// layer of its own and takes no input.
/// </summary>
public sealed class GumInline<T>(Func<T> create) where T : GraphicalUiElement
{
    private T? _root;
    private Layer? _layer;

    /// <summary>The element (made when first needed, once Gum is ready), to style before drawing it.</summary>
    public T Element
    {
        get
        {
            if (_root == null)
            {
                _layer = SystemManagers.Default.Renderer.AddLayer();
                _root = create();
                _root.AddToManagers(SystemManagers.Default, _layer);
            }
            return _root;
        }
    }

    /// <summary>Draws the element over <paramref name="area"/> (the game's UI units, two a UI pixel), on whole UI pixels, in the game's begun sprite batch.</summary>
    public void Draw(SpriteBatch batch, Rectangle area)
    {
        if (!GumHost.IsInitialized)
            return;
        T root = Element;
        float x = MathF.Floor(area.X / 2f), y = MathF.Floor(area.Y / 2f);
        float width = MathF.Round(area.Right / 2f) - x, height = MathF.Round(area.Bottom / 2f) - y;
        if (root.X != x || root.Y != y || root.Width != width || root.Height != height)
        {
            root.X = x;
            root.Y = y;
            root.Width = width;
            root.Height = height;
        }
        GumHost.DrawLayer(batch, _layer!);
    }
}
