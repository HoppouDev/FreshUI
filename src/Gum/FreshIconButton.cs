using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.GumUi;

/// <summary>
/// How a <see cref="FreshIconButton"/> looks: its icon (a texture, or a region of one) and, optionally,
/// an overlay drawn over it while it's hovered, such as an outline.
/// </summary>
record FreshIconButtonLook(
    Texture2D Icon,
    Rectangle? IconSource = null,
    Texture2D? HoverOverlay = null,
    Rectangle? HoverOverlaySource = null);

/// <summary>
/// A button that's just an icon, with no background. It has a <see cref="Look"/> and, for a toggle,
/// a <see cref="CheckedLook"/>; while hovered it draws the look's overlay over the icon, and each state
/// (hovered, pushed, disabled) can tint the icon. Sizes are in UI pixels (see UiPixels): the icon is
/// drawn at <see cref="TexelSize"/> UI pixels per texel, its overlay centred over it on the UI's grid.
/// </summary>
class FreshIconButton : FrameworkElement
{
    private readonly SpriteRuntime _icon;
    private readonly SpriteRuntime _overlay;

    private FreshIconButtonLook? _look;
    private FreshIconButtonLook? _checkedLook;
    private float _texelSize = 0.5f;
    private Color _hoverOverlayColor = Color.White;
    private Color _hoverColor = Color.White;
    private Color _pushedColor = Color.White;
    private Color _disabledColor = new(128, 128, 128);
    private bool _isChecked;
    private bool _isHovered;
    private bool _isPushed;

    public FreshIconButton() : base(new ContainerRuntime())
    {
        Visual.HasEvents = true;
        Visual.WidthUnits = DimensionUnitType.Absolute;
        Visual.HeightUnits = DimensionUnitType.Absolute;

        _icon = CreateSprite();
        _overlay = CreateSprite();
        Visual.Children.Add(_icon);
        Visual.Children.Add(_overlay);

        Visual.RollOn += (_, _) => { _isHovered = true; Refresh(); };
        Visual.RollOff += (_, _) => { _isHovered = false; Refresh(); };
        Visual.Push += (_, _) => { _isPushed = true; Refresh(); };
        Visual.RemovedAsPushed += (_, _) => { _isPushed = false; Refresh(); };
        Visual.Click += (_, _) =>
        {
            if (!IsEnabled)
            {
                return;
            }
            if (IsToggle)
            {
                IsChecked = !IsChecked;
            }
            Click?.Invoke(this, EventArgs.Empty);
        };
        Refresh();
    }

    /// <summary>Raised when the button is clicked while enabled, after a toggle has flipped <see cref="IsChecked"/>.</summary>
    public event EventHandler? Click;

    /// <summary>Raised when <see cref="IsChecked"/> changes.</summary>
    public event EventHandler? CheckedChanged;

    /// <summary>How the button looks, or while <see cref="IsChecked"/>, when it has no <see cref="CheckedLook"/>.</summary>
    public FreshIconButtonLook? Look
    {
        get => _look;
        set { _look = value; Refresh(); }
    }

    /// <summary>How the button looks while <see cref="IsChecked"/>, or null to keep its <see cref="Look"/>.</summary>
    public FreshIconButtonLook? CheckedLook
    {
        get => _checkedLook;
        set { _checkedLook = value; Refresh(); }
    }

    /// <summary>Whether clicking the button flips <see cref="IsChecked"/>.</summary>
    public bool IsToggle { get; set; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }
            _isChecked = value;
            Refresh();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>UI pixels per texel: ½ for art drawn as 2×2 texels per pixel, 1 for a texel per pixel.</summary>
    public float TexelSize
    {
        get => _texelSize;
        set { _texelSize = value; Refresh(); }
    }

    /// <summary>The hover overlay's tint.</summary>
    public Color HoverOverlayColor
    {
        get => _hoverOverlayColor;
        set { _hoverOverlayColor = value; Refresh(); }
    }

    /// <summary>The icon's tint while hovered.</summary>
    public Color HoverColor
    {
        get => _hoverColor;
        set { _hoverColor = value; Refresh(); }
    }

    /// <summary>The icon's tint while pushed.</summary>
    public Color PushedColor
    {
        get => _pushedColor;
        set { _pushedColor = value; Refresh(); }
    }

    /// <summary>The icon's tint while disabled.</summary>
    public Color DisabledColor
    {
        get => _disabledColor;
        set { _disabledColor = value; Refresh(); }
    }

    public override void UpdateState()
    {
        base.UpdateState();
        // The base constructor can ask before this one has made the sprites.
        if (_icon != null)
        {
            Refresh();
        }
    }

    private static SpriteRuntime CreateSprite() => new()
    {
        WidthUnits = DimensionUnitType.Absolute,
        HeightUnits = DimensionUnitType.Absolute,
        TextureAddress = TextureAddress.Custom,
        Visible = false,
    };

    private void Refresh()
    {
        FreshIconButtonLook? look = _isChecked && _checkedLook != null ? _checkedLook : _look;
        _icon.Visible = look != null;
        _overlay.Visible = false;
        if (look == null)
        {
            return;
        }

        Rectangle icon = Show(_icon, look.Icon, look.IconSource);
        float width = icon.Width * _texelSize;
        float height = icon.Height * _texelSize;
        Visual.Width = width;
        Visual.Height = height;
        _icon.Color = !IsEnabled ? _disabledColor
            : _isPushed && _isHovered ? _pushedColor
            : _isHovered ? _hoverColor
            : Color.White;

        if (look.HoverOverlay != null && _isHovered && IsEnabled)
        {
            Rectangle overlay = Show(_overlay, look.HoverOverlay, look.HoverOverlaySource);
            _overlay.Visible = true;
            _overlay.X = MathF.Round((width - overlay.Width * _texelSize) / 2);
            _overlay.Y = MathF.Round((height - overlay.Height * _texelSize) / 2);
            _overlay.Color = _hoverOverlayColor;
        }
    }

    // Shows a texture's region on a sprite at the button's texel size, returning the region.
    private Rectangle Show(SpriteRuntime sprite, Texture2D texture, Rectangle? source)
    {
        Rectangle region = source ?? texture.Bounds;
        sprite.Texture = texture;
        sprite.TextureLeft = region.X;
        sprite.TextureTop = region.Y;
        sprite.TextureWidth = region.Width;
        sprite.TextureHeight = region.Height;
        sprite.Width = region.Width * _texelSize;
        sprite.Height = region.Height * _texelSize;
        return region;
    }
}
