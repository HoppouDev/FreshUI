using FreshUI.GumUi;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace FreshUI.Components;

/// <summary>
/// A <see cref="FreshButton"/> with an icon before its text, the pair centred on the button. The icon is
/// a texture or a region of one, drawn at <see cref="IconTexelSize"/> UI pixels per texel (half a pixel,
/// for the game's 2×2-texel art) and dimmed with the text when disabled. With no
/// text, the icon alone is centred.
/// </summary>
class FreshIconTextButton : FreshButton
{
    private readonly ContainerRuntime _content;
    private readonly SpriteRuntime _icon;
    private Rectangle? _iconSource;
    private float _iconTexelSize = 0.5f;

    public FreshIconTextButton()
    {
        var visual = (ButtonVisual)Visual;
        TextRuntime text = visual.TextInstance;

        // The icon and text in a row, centred: the text sized to itself rather than spanning the button.
        _content = new ContainerRuntime
        {
            XUnits = GeneralUnitType.PixelsFromMiddle,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            XOrigin = HorizontalAlignment.Center,
            YOrigin = VerticalAlignment.Center,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            ChildrenLayout = ChildrenLayout.LeftToRightStack,
            StackSpacing = 3,
            HasEvents = false,
        };
        _icon = new SpriteRuntime
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            YUnits = GeneralUnitType.PixelsFromMiddle,
            YOrigin = VerticalAlignment.Center,
            Visible = false,
        };
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.XUnits = GeneralUnitType.PixelsFromSmall;
        text.XOrigin = HorizontalAlignment.Left;
        text.X = 0;
        visual.Children.Remove(text);
        _content.Children.Add(_icon);
        _content.Children.Add(text);
        // The button's states colour the text; the icon follows (dim when disabled), and an empty text
        // drops out of the row so the icon centres alone.
        text.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TextRuntime.Color))
                _icon.Color = IconTint(text.Color);
            else if (e.PropertyName == nameof(TextRuntime.Text))
                text.Visible = !string.IsNullOrEmpty(text.Text);
        };
        _icon.Color = IconTint(text.Color);
        // Over the background, under the focus indicator.
        visual.Children.Insert(visual.Children.IndexOf(visual.Background) + 1, _content);
    }

    /// <summary>The icon's texture, or null for none.</summary>
    public Texture2D? Icon
    {
        get => _icon.Texture;
        set { _icon.Texture = value; Refresh(); }
    }

    /// <summary>The icon's region of <see cref="Icon"/>, or null for the whole texture.</summary>
    public Rectangle? IconSource
    {
        get => _iconSource;
        set { _iconSource = value; Refresh(); }
    }

    /// <summary>UI pixels per texel of the icon: 0.5 for the game's 2×2-texel art, 1 for art with a texel per pixel.</summary>
    public float IconTexelSize
    {
        get => _iconTexelSize;
        set { _iconTexelSize = MathF.Max(0.01f, value); Refresh(); }
    }

    /// <summary>UI pixels between the icon and the text.</summary>
    public float IconSpacing
    {
        get => _content.StackSpacing;
        set => _content.StackSpacing = value;
    }

    // The text's greys (dim when disabled) tint the icon; its colours (gold when focused) don't.
    private static Color IconTint(Color text) => text.R == text.G && text.G == text.B ? text : Color.White;

    private void Refresh()
    {
        Texture2D? texture = _icon.Texture;
        _icon.Visible = texture != null;
        if (texture == null)
            return;
        Rectangle source = _iconSource ?? texture.Bounds;
        _icon.TextureAddress = TextureAddress.Custom;
        _icon.TextureLeft = source.X;
        _icon.TextureTop = source.Y;
        _icon.TextureWidth = source.Width;
        _icon.TextureHeight = source.Height;
        _icon.Width = source.Width * _iconTexelSize;
        _icon.Height = source.Height * _iconTexelSize;
    }
}
