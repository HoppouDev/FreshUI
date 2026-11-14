using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;

namespace FreshUI.Screens;

/// <summary>
/// What goes with a shop and the reforge and Guide slots (Aether's <see cref="UIShopHeader"/>,
/// <see cref="UIReforgeCost"/> and <see cref="UISingleSlot"/>): the shop's name over its slots, the price
/// adjustment from the NPC's happiness (its face and percentage) and the player's savings beside them; the
/// reforge slot and the Guide's slot where the game places them, handled by <c>ItemSlot.Handle</c>; and the
/// reforge price beside its slot. Savings and the price (coins in their colours, the game's chat text) are
/// drawn by the game in the HUD's game view.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> ShopHeaderAsked = GumRoutes.GameScreen<UIShopHeader>();
    private static readonly Func<bool> ReforgeCostAsked = GumRoutes.GameScreen<UIReforgeCost>();
    private static readonly List<UISingleSlot> SinglesAsked = RouteSingleSlots();
    private static readonly FieldInfo SinglePosition = typeof(UISingleSlot).GetField("_position", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo SingleScale = typeof(UISingleSlot).GetField("_scale", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ReforgeX = typeof(UIReforgeCost).GetField("_x", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ReforgeY = typeof(UIReforgeCost).GetField("_y", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static List<UISingleSlot> RouteSingleSlots()
    {
        List<UISingleSlot> asked = [];
        GumRoutes.GameScreens(screen => screen is UISingleSlot, screen =>
        {
            if (!asked.Contains((UISingleSlot)screen))
                asked.Add((UISingleSlot)screen);
        });
        return asked;
    }

    private TextRuntime? _shopName, _happinessText;
    private SpriteRuntime? _happinessFace;
    private float _shownAdjustment = float.NaN;
    private readonly Item[] _single = new Item[1];
    private readonly object _reforgeOwner = new(), _guideOwner = new();
    // Drawn by the game in the HUD's game view this frame (NaN: not): savings, and the reforge price.
    private Vector2 _savingsAt = new(float.NaN), _reforgeCostAt = new(float.NaN);
    private bool _savingsHorizontal;
    private string? _costLabel, _costLabelSource, _priceText;
    private long _pricedAt = -1;

    private void PresentShopInfo()
    {
        _savingsAt = _reforgeCostAt = new Vector2(float.NaN);
        bool header = IsReady && ShopHeaderAsked();
        if (_shopName == null && header)
            CreateShopHeader();
        if (_shopName != null && _shopName.Visible != header)
        {
            _shopName.Visible = header;
            _happinessFace!.Visible = header;
            _happinessText!.Visible = header;
        }
        if (header)
            PresentShopHeader();
        if (IsReady && ReforgeCostAsked())
        {
            int x = (int)ReforgeX.GetValue(UIReforgeCost.Instance)!, y = (int)ReforgeY.GetValue(UIReforgeCost.Instance)!;
            _reforgeCostAt = new Vector2(x, y);
            if (Main.reforgeItem.type > 0)
            {
                _savingsAt = new Vector2(x + 130, Main.instance.invBottom);
                _savingsHorizontal = true;
            }
        }
        foreach (UISingleSlot single in SinglesAsked)
            PresentSingleSlot(single);
        SinglesAsked.Clear();
    }

    private void CreateShopHeader()
    {
        _shopName = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 9.5f,
            Color = Color.White,
            HasEvents = false,
        };
        GumText.SetOutline(_shopName, 0.5f, Color.Black);
        Children.Add(_shopName);
        Texture2D faces = TextureAssets.NPCHappiness.Value;
        _happinessFace = new SpriteRuntime
        {
            Texture = faces,
            TextureAddress = Gum.Managers.TextureAddress.Custom,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
        };
        Children.Add(_happinessFace);
        _happinessText = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 9.5f,
            Color = Color.White,
            HasEvents = false,
        };
        GumText.SetOutline(_happinessText, 0.5f, Color.Black);
        Children.Add(_happinessText);
    }

    // The shop's name over its slots (as a chest's); beside them the happiness face and percentage, and under those the savings.
    private void PresentShopHeader()
    {
        float left = GroupOrigin.X + Pitch, top = GroupOrigin.Y;
        SetText(_shopName!, Lang.inter[28].Value);
        if (_shopName!.X != left || _shopName.Y != top - 13)
        {
            _shopName.X = left;
            _shopName.Y = top - 13;
        }
        float adjustment = Main.LocalPlayer.currentShoppingSettings.PriceAdjustment;
        if (adjustment != _shownAdjustment)
        {
            _shownAdjustment = adjustment;
            int frame = adjustment <= 0.82f ? 0 : adjustment <= 1f ? 1 : adjustment <= 1.1f ? 2 : 3;
            Rectangle source = TextureAssets.NPCHappiness.Value.Frame(4, 1, frame);
            SpriteRuntime face = _happinessFace!;
            face.TextureLeft = source.X;
            face.TextureTop = source.Y;
            face.TextureWidth = source.Width;
            face.TextureHeight = source.Height;
            face.Width = MathF.Round(source.Width * Components.FreshStyle.Texel);
            face.Height = MathF.Round(source.Height * Components.FreshStyle.Texel);
            _happinessText!.Text = adjustment.ToString("P0");
        }
        float x = GroupOrigin.X + 11 * Pitch + 2;
        if (_happinessFace!.X != x || _happinessFace.Y != top)
        {
            _happinessFace.X = x;
            _happinessFace.Y = top;
            _happinessText!.X = x + _happinessFace.Width + 3;
            _happinessText.Y = top + MathF.Round((_happinessFace.Height - 9) / 2);
        }
        _savingsAt = new Vector2(x * 2, (top + _happinessFace.Height + 2) * 2);
        _savingsHorizontal = false;
    }

    // The reforge or Guide slot where the game placed it, handled as the game's (hovering it hides the crafting list).
    private void PresentSingleSlot(UISingleSlot single)
    {
        bool reforge = single == UISingleSlot.Reforge;
        var position = (Vector2)SinglePosition.GetValue(single)!;
        float scale = (float)SingleScale.GetValue(single)!;
        if (float.IsNaN(position.X))
            return;
        int context = reforge ? 5 : 7;
        _single[0] = reforge ? Main.reforgeItem : Main.guideItem;
        float size = TextureAssets.InventoryBack.Width() * scale;
        if (new Rectangle((int)position.X, (int)position.Y, (int)size, (int)size).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            Main.craftingHide = true;
            Main.LocalPlayer.mouseInterface = true;
            Main.inventoryScale = scale;
            ItemSlot.Handle(_single, context, 0);
            SetSingle(reforge, _single[0]);
        }
        Main.inventoryScale = scale;
        Present(reforge ? _reforgeOwner : _guideOwner, _single, context, 0, position, reforge ? SetReforge : SetGuide, HideCrafting);
    }

    private static readonly Action<Item> SetReforge = item => Main.reforgeItem = item, SetGuide = item => Main.guideItem = item;
    private static readonly Func<bool> HideCrafting = () => Main.craftingHide = true;

    private static void SetSingle(bool reforge, Item item)
    {
        if (reforge)
            Main.reforgeItem = item;
        else
            Main.guideItem = item;
    }

    // In the HUD's game view: the savings and the reforge price, as the game draws them.
    private void DrawShopInfo(SpriteBatch batch)
    {
        if (!float.IsNaN(_savingsAt.X))
            ItemSlot.DrawSavings(batch, _savingsAt.X, _savingsAt.Y, _savingsHorizontal);
        if (float.IsNaN(_reforgeCostAt.X))
            return;
        var position = new Vector2(_reforgeCostAt.X + 50, _reforgeCostAt.Y);
        var color = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
        if (Main.reforgeItem.type <= 0)
        {
            ChatManager.DrawColorCodedStringWithShadow(batch, FontAssets.MouseText.Value, Lang.inter[20].Value, position, color, 0f, Vector2.Zero, Vector2.One);
            return;
        }
        string cost = Lang.inter[46].Value;
        if (!ReferenceEquals(cost, _costLabelSource))
        {
            _costLabelSource = cost;
            _costLabel = cost + ": ";
        }
        long price = UIReforgeCost.Price();
        if (price != _pricedAt || _priceText == null)
        {
            _pricedAt = price;
            _priceText = UIReforgeCost.CoinText(price);
        }
        ChatManager.DrawColorCodedStringWithShadow(batch, FontAssets.MouseText.Value, _costLabel, position, color, 0f, Vector2.Zero, Vector2.One);
        position.X += FontAssets.MouseText.Value.MeasureString(_costLabel).X;
        ChatManager.DrawColorCodedStringWithShadow(batch, FontAssets.MouseText.Value, _priceText, position, Color.White, 0f, Vector2.Zero, Vector2.One);
    }
}
