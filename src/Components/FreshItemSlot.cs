using FreshUI.GumUi;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace FreshUI.Components;

/// <summary>What a slot holds, which picks its background.</summary>
enum FreshItemSlotKind
{
    Inventory,
    /// <summary>The first row, whose slots are numbered.</summary>
    Hotbar,
    Coin,
    Ammo,
    Trash,
    /// <summary>
    /// An open container's slots: a chest's, a piggy bank's. Containers differ, so each sets its own
    /// <see cref="FreshItemSlot.BackgroundPath"/>.
    /// </summary>
    Container,
    Equipment,
    Dye,
    Shop,
    Crafting,
}

/// <summary>
/// An inventory slot: a nine-sliced background, an item icon fitted inside the border, its stack
/// count bottom left and, in a hotbar row, its number top left, both in bordered distance field text
/// (see GumText). Sizes are in UI pixels (see UiPixels), one per pixel of the slot and item art
/// (2×2 texels), so the art stays on the UI's grid.
/// </summary>
class FreshItemSlot : FrameworkElement
{
    /// <summary>The background for each kind, relative to the executable. Set them before creating slots.</summary>
    public static Dictionary<FreshItemSlotKind, string> BackgroundPaths { get; } = [];

    /// <summary>
    /// Drawn over a highlighted slot's background (<see cref="IsHighlighted"/>; art the slot's size,
    /// sliced like the backgrounds), relative to the executable, or null for none. Set it before
    /// creating slots.
    /// </summary>
    public static string? HighlightOverlayPath { get; set; }

    /// <summary>Drawn instead of <see cref="HighlightOverlayPath"/> over the selected hotbar slot, or null for that.</summary>
    public static string? SelectedOverlayPath { get; set; }

    // The slot art: 52 texels square (26 UI pixels), with rounded corners and a border 8 texels in
    // from each edge.
    private const int ArtSize = 52;
    private const int Corner = 8;

    // Icons keep 1.5 UI pixels from the slot's edges.
    private const float IconMargin = 1.5f;

    // The background's tint: white at about 86%, so what's behind shows through a little.
    private static readonly Color BackgroundColor = new(255, 255, 255, 220);
    private static readonly Color NumberColor = new(255, 255, 255, 210);

    // The text, in texels of the slot art at its scale: 16.75 per em with a 1-texel border, its
    // baseline 13.62 below where the slot art places its top, and its ink 1 texel in from the pen.
    private const float TextSize = 16.75f;
    private const float TextBaseline = 13.62f;
    private const float TextInset = 1;
    private const float StackLeft = 10, StackTop = 26;
    private const float NumberLeft = 6, NumberTop = 4, SelectedNumberLift = 2;

    private static readonly Dictionary<string, Texture2D> Textures = [];
    private static readonly string[] SlotBackgrounds = Enumerable.Range(0, 25)
        .Select(index => "Images/Inventory_Back" + (index == 0 ? "" : index.ToString())).ToArray();

    private readonly NineSliceRuntime _background;
    private readonly NineSliceRuntime _highlightOverlay;
    private readonly SpriteRuntime _icon;
    private readonly TextRuntime _stack;
    private readonly TextRuntime _number;

    private float _scale = DefaultScale;
    private FreshItemSlotKind _kind;
    private string? _backgroundPath;
    private bool _isHighlighted;
    private bool _isSelected;
    private Texture2D? _item;
    private Rectangle? _itemSource;
    private Texture2D? _placeholder;
    private Rectangle? _placeholderSource;
    private Color _iconColor = Color.White;
    private Color _backgroundColor = BackgroundColor;
    private int _count;
    private int? _hotbarIndex;

    // What the slot last showed: SetItem runs every frame, and Gum's setters notify (and allocate) even
    // for unchanged values.
    private (float, FreshItemSlotKind, string?, bool, bool, Texture2D?, Rectangle?, Texture2D?, Rectangle?, Color, Color, int, int?, int)? _shown;

    /// <summary>Live HUD activation for remote automation; physical input stays in Aether.</summary>
    internal Func<bool, bool>? HudAction { get; set; }

    /// <summary>Updates a live item in one pass, including animated icons and empty equipment silhouettes.</summary>
    internal void SetItem(Item item, int context, int index, float scale)
    {
        _scale = scale;
        _kind = context switch
        {
            1 => FreshItemSlotKind.Coin,
            2 => FreshItemSlotKind.Ammo,
            3 or 4 or 32 => FreshItemSlotKind.Container,
            6 => FreshItemSlotKind.Trash,
            12 or 33 => FreshItemSlotKind.Dye,
            8 or 9 or 10 or 11 or 16 or 17 or 18 or 19 or 20 => FreshItemSlotKind.Equipment,
            13 or 36 => FreshItemSlotKind.Hotbar,
            15 => FreshItemSlotKind.Shop,
            22 or 35 => FreshItemSlotKind.Crafting,
            _ => FreshItemSlotKind.Inventory,
        };
        bool hotbar = context is 13 or 36 || context == 0 && index < 10;
        bool selected = hotbar && index == Main.LocalPlayer.selectedItem && !Main.LocalPlayer.selectedItemState.HasActiveOverride;
        _hotbarIndex = hotbar && index < 10 ? index : null;
        _isSelected = selected;
        _isHighlighted = selected || !item.IsAir && item.favorited ||
            ItemSlot.ShouldHighlightSlotForMouseItem(context, index, Main.mouseItem);
        int background = context switch
        {
            1 or 15 => 6,
            2 => 8,
            3 => 5,
            4 or 32 => 2,
            5 or 7 or 22 => 4,
            35 => 2,
            6 => 7,
            8 or 9 or 10 or 11 or 12 => 13,
            16 or 17 or 18 or 19 or 20 => 3,
            33 => 12,
            _ => 0,
        };
        if (hotbar)
            background = 9; // Selected: the same, under SelectedOverlayPath's outline.
        else if (!item.IsAir && item.favorited)
            background = context == 32 ? 19 : 10;
        else if (!item.IsAir && item.newAndShiny && ItemSlot.Options.HighlightNewItems)
            background = 15;
        if (context is 22 or 35 && ItemSlot.DrawGoldBGForCraftingMaterial)
        {
            background = 14;
            ItemSlot.DrawGoldBGForCraftingMaterial = false;
        }
        _backgroundPath = SlotBackgrounds[background];
        _backgroundColor = context is 8 or 9 or 10 or 11 or 12
            ? ItemSlot.GetColorByLoadout(index, context) : Main.inventoryBack;
        _count = item.IsAir ? 0 : item.stack;
        _item = null;
        _itemSource = null;
        _placeholder = null;
        _placeholderSource = null;
        if (!item.IsAir)
        {
            Main.instance.LoadItem(item.type);
            _item = TextureAssets.Item[item.type].Value;
            _itemSource = Main.itemAnimations[item.type]?.GetFrame(_item) ?? _item.Bounds;
            Color light = Color.White;
            ItemSlot.GetItemLight(ref light, item);
            _iconColor = item.GetAlpha(light);
        }
        else if (context == 6)
            _placeholder = TextureAssets.Trash.Value;
        else
        {
            int silhouette = context switch
            {
                8 when index < 3 => index * 6,
                9 when index is >= 10 and < 13 => (index - 10) * 6 + 3,
                10 => 11,
                11 => 2,
                12 or 33 => 1,
                16 => 4,
                17 => 13,
                18 => 7,
                19 => 10,
                20 => 17,
                7 => 18,
                _ => -1,
            };
            if (silhouette >= 0)
            {
                _placeholder = TextureAssets.Extra[54].Value;
                Rectangle frame = _placeholder.Frame(3, 7, silhouette % 3, silhouette / 3);
                frame.Width -= 2;
                frame.Height -= 2;
                _placeholderSource = frame;
            }
        }
        Refresh();
    }

    public FreshItemSlot() : base(new ContainerRuntime())
    {
        Visual.HasEvents = true;
        Visual.WidthUnits = DimensionUnitType.Absolute;
        Visual.HeightUnits = DimensionUnitType.Absolute;

        _background = CreateSlotArt();
        // Over the background, under the icon and text.
        _highlightOverlay = CreateSlotArt();
        _highlightOverlay.Color = Color.White;
        _highlightOverlay.Visible = false;
        _icon = new SpriteRuntime
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Visible = false,
        };
        _stack = CreateText();
        _number = CreateText();

        Visual.Children.Add(_background);
        Visual.Children.Add(_highlightOverlay);
        Visual.Children.Add(_icon);
        Visual.Children.Add(_stack);
        Visual.Children.Add(_number);
        Refresh();
    }

    // Slot-sized art (a background or an overlay) filling the slot. Corners and border keep their own
    // size (a UI pixel per 2 texels); only the flat middle stretches or shrinks.
    private static NineSliceRuntime CreateSlotArt() => new()
    {
        WidthUnits = DimensionUnitType.RelativeToParent,
        HeightUnits = DimensionUnitType.RelativeToParent,
        Width = 0,
        Height = 0,
        TextureAddress = TextureAddress.EntireTexture,
        CustomFrameTextureCoordinateWidth = Corner,
        BorderScale = 0.5f,
        Color = BackgroundColor,
    };

    public event EventHandler Click
    {
        add => Visual.Click += value;
        remove => Visual.Click -= value;
    }

    /// <summary>The slot's scale: 1 is the art's 26 UI pixels across. Its size is rounded to whole UI pixels.</summary>
    public float Scale
    {
        get => _scale;
        set { _scale = MathF.Max(0.1f, value); Refresh(); }
    }

    /// <summary>The slot's size in UI pixels at its <see cref="Scale"/>.</summary>
    public float Size => SizeAt(_scale);

    /// <summary>A slot's size in UI pixels at <paramref name="scale"/>: whole UI pixels across.</summary>
    public static float SizeAt(float scale) => MathF.Round(ArtSize / 2 * scale);

    // UI pixels per texel of the slot art at the slot's scale, for the text's metrics.
    private float TexelSize => _scale / 2;

    /// <summary>The scale slots are created at.</summary>
    public const float DefaultScale = 0.85f;

    /// <summary>
    /// The background, relative to the executable, in place of its <see cref="Kind"/>'s, or null for
    /// that.
    /// </summary>
    public string? BackgroundPath
    {
        get => _backgroundPath;
        set { _backgroundPath = value; Refresh(); }
    }

    public FreshItemSlotKind Kind
    {
        get => _kind;
        set { _kind = value; Refresh(); }
    }

    /// <summary>Whether the slot shows the highlight overlay (the selected item, or a favourite) and a brighter, raised number.</summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set { _isHighlighted = value; Refresh(); }
    }

    /// <summary>The item's icon, or null for an empty slot.</summary>
    public Texture2D? Item
    {
        get => _item;
        set { _item = value; Refresh(); }
    }

    /// <summary>The icon's frame within <see cref="Item"/>, or null for the whole texture.</summary>
    public Rectangle? ItemSource
    {
        get => _itemSource;
        set { _itemSource = value; Refresh(); }
    }

    /// <summary>A faint silhouette shown while the slot is empty (a trash can, an armour piece).</summary>
    public Texture2D? Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; Refresh(); }
    }

    /// <summary>How many items are stacked in the slot; shown when more than one.</summary>
    public int Count
    {
        get => _count;
        set { _count = Math.Max(0, value); Refresh(); }
    }

    /// <summary>The slot's place in a hotbar row, 0 to 9, shown as its key (1 to 9, then 0), or null for none.</summary>
    public int? HotbarIndex
    {
        get => _hotbarIndex;
        set { _hotbarIndex = value; Refresh(); }
    }

    private static TextRuntime CreateText()
    {
        var text = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            Visible = false,
        };
        text.HasEvents = false;
        return text;
    }

    private void Refresh()
    {
        var state = (_scale, _kind, _backgroundPath, _isHighlighted, _isSelected, _item, _itemSource, _placeholder,
            _placeholderSource, _iconColor, _backgroundColor, _count, _hotbarIndex, UiPixels.Scale);
        if (_shown is { } shown && shown.Equals(state))
        {
            return;
        }
        _shown = state;

        float size = Size;
        Visual.Width = size;
        Visual.Height = size;

        _background.Texture = LoadTexture(_backgroundPath ?? BackgroundPaths.GetValueOrDefault(_kind));
        _background.Color = _backgroundColor;
        _highlightOverlay.Texture = _isHighlighted
            ? LoadTexture(_isSelected ? SelectedOverlayPath ?? HighlightOverlayPath : HighlightOverlayPath) : null;
        _highlightOverlay.Visible = _highlightOverlay.Texture != null;

        RefreshIcon(size);
        PlaceText(_stack, _count > 1 ? _count.ToString() : null, StackLeft, StackTop, Color.White);
        PlaceText(_number, _hotbarIndex is int index ? ((index + 1) % 10).ToString() : null,
            NumberLeft, _isHighlighted ? NumberTop - SelectedNumberLift : NumberTop,
            _isHighlighted ? Color.White : NumberColor);
    }

    private void RefreshIcon(float size)
    {
        Texture2D? texture = _item ?? _placeholder;
        _icon.Visible = texture != null;
        if (texture == null)
        {
            return;
        }

        Rectangle frame = (_item != null ? _itemSource : _placeholderSource) ?? texture.Bounds;
        _icon.Texture = texture;
        _icon.TextureAddress = TextureAddress.Custom;
        _icon.TextureLeft = frame.X;
        _icon.TextureTop = frame.Y;
        _icon.TextureWidth = frame.Width;
        _icon.TextureHeight = frame.Height;

        float perTexel = IconScale(frame, size);
        float width = frame.Width * perTexel;
        float height = frame.Height * perTexel;
        _icon.Width = width;
        _icon.Height = height;
        // Centred on the UI's grid at full size; shrunk, on whole screen pixels at least.
        Func<float, float> snap = perTexel == 0.5f ? MathF.Round : UiPixels.Snap;
        _icon.X = snap((size - width) / 2);
        _icon.Y = snap((size - height) / 2);
        // A placeholder is a silhouette: white at about 40%.
        _icon.Color = _item != null ? _iconColor : new Color(255, 255, 255, 100);
    }

    /// <summary>
    /// UI pixels per texel for an icon: a UI pixel per art pixel (2×2 texels), or, if that doesn't fit
    /// inside the border, the most whole screen pixels per art pixel that do, so its pixels stay even.
    /// </summary>
    private static float IconScale(Rectangle frame, float size)
    {
        float largest = Math.Max(frame.Width, frame.Height);
        float limit = size - IconMargin * 2 + 0.01f;
        int scale = UiPixels.Scale;
        for (int screenPixels = scale; screenPixels > 1; screenPixels--)
        {
            float perTexel = screenPixels / (2f * scale);
            if (perTexel * largest <= limit)
            {
                return perTexel;
            }
        }
        return 1 / (2f * scale);
    }

    private void PlaceText(TextRuntime text, string? value, float left, float top, Color color)
    {
        text.Visible = value != null;
        if (value == null)
        {
            return;
        }

        float fontSize = TextSize * TexelSize;
        text.Text = value;
        text.FontSize = fontSize;
        text.Color = color;
        GumText.SetOutline(text, TexelSize, Color.Black);
        text.HasDropshadow = true;
        text.DropshadowColor = Color.Black;
        text.DropshadowBlur = 1;
        text.DropshadowOffsetX = 0;
        text.DropshadowOffsetY = 1;
        text.DropshadowAlpha = 100;
        text.X = (left + TextInset) * TexelSize;
        // Placed by its baseline, whatever the font's own metrics.
        text.Y = (top + TextBaseline) * TexelSize - fontSize * GumText.Baseline;
    }

    private static Texture2D? LoadTexture(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        if (!Textures.TryGetValue(path, out Texture2D? texture))
        {
            texture = GumTextures.Load(path);
            Textures[path] = texture;
        }
        return texture;
    }
}
