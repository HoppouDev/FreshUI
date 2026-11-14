using FreshUI.Components;
using FreshUI.GumUi;
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.UI.Gamepad;
using Aether.UI.Game;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Screens;

/// <summary>
/// The live inventory in the original Gum layout: four container rows above four inventory rows,
/// coins on the left, ammo on the right, then the hotbar between its lock and trash. Aether handles
/// the live slots, with its hit boxes moved to the same grid before it processes mouse input.
/// </summary>
public sealed partial class HudScreen : ContainerRuntime
{
    // The original InventoryScreen dimensions, including the reserved space for a closed container.
    private const float Pitch = 23, Cell = 22, GroupWidth = 275, GroupHeight = 206;
    private const float InventoryTop = 4 * Pitch, HotbarTop = InventoryTop + 4 * Pitch;
    private const float BottomMargin = 10;
    private static HudScreen? _current;
    private readonly Dictionary<object, SlotBinding> _slots = [];
    private readonly List<object> _expired = [];
    private readonly TextRuntime _heldName;
    private bool _namePresented;
    private readonly FreshIconButton _hotbarLock;
    // Game art drawn over the slots (the equipment slots' corner toggles), this frame's.
    private readonly FreshGameView _over;
    private readonly GameIconButtons _icons;
    private readonly CraftingWindow _crafting;
    private readonly StylistWindow _stylist = new();
    private readonly DresserWindow _dresser = new();
    private readonly List<(Texture2D Texture, Vector2 Position, Color Color)> _overDraws = [];

    public HudScreen()
    {
        WidthUnits = DimensionUnitType.RelativeToParent;
        HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        _heldName = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 9.5f,
            HasEvents = false,
            Visible = false,
        };
        _heldName.Anchor(Gum.Wireframe.Anchor.Top);
        GumText.SetOutline(_heldName, 0.5f, Color.Black);
        Children.Add(_heldName);
        _hotbarLock = new FreshIconButton
        {
            IsToggle = true,
            Look = LockLook(GumTextures.Load("Images/Lock_1")),
            CheckedLook = LockLook(GumTextures.Load("Images/Lock_0")),
            HoverOverlayColor = new Color(255, 231, 69),
        };
        _hotbarLock.Click += (_, _) => Main.LocalPlayer.hbLocked = _hotbarLock.IsChecked;
        Children.Add(_hotbarLock.Visual);
        // Aether's icon buttons (side buttons, builder toggles, page icons…), under the slots' corner toggles.
        _icons = new GameIconButtons();
        Children.Add(_icons);
        _crafting = new CraftingWindow();
        Children.Add(_crafting);
        Children.Add(_stylist);
        Children.Add(_dresser);
        _over = new FreshGameView();
        _over.Draw = (batch, _) =>
        {
            DrawEquipmentExtras(batch);
            DrawShopInfo(batch);
            foreach ((Texture2D texture, Vector2 position, Color color) in _overDraws)
                batch.Draw(texture, position, color);
            _overDraws.Clear();
        };
        Children.Add(_over);
        FreshTooltip.Attach(_hotbarLock.Visual, () => Main.LocalPlayer.hbLocked ? "Hotbar locked" : "Hotbar unlocked");
        _current = this;
    }

    // Aether's inventory screens this screen replaces: shown (the game asks for them) while the inventory is open.
    private static readonly Func<bool> GridAsked = GumRoutes.GameScreen<UIInventoryGrid>();
    private static readonly Func<bool> CoinsAsked = GumRoutes.GameScreen<UICoinAmmoSlots>();
    private static readonly Func<bool> TrashAsked = GumRoutes.GameScreen<UITrashSlot>();
    private static readonly Func<bool> HotbarAsked = GumRoutes.GameScreen<UIHotbar>();

    /// <summary>Registers the routes of the game's screens this screen replaces (from the mod initializer).</summary>
    public static void RegisterRoutes() => _ = GridAsked;

    // The slots' owners (stable keys for their bindings), and the trash's one-item array.
    private readonly object[] _owners = Enumerable.Range(0, 120).Select(_ => new object()).ToArray();
    private readonly Item[] _trash = new Item[1];

    // The inventory's slots, from Aether's screens (UIInventoryGrid, UICoinAmmoSlots, UITrashSlot): presented,
    // and handled by the game's ItemSlot.Handle while the cursor (the mouse, or the gamepad's) is on one.
    private void PresentInventory()
    {
        if (!IsReady)
            return;
        Player player = Main.LocalPlayer;
        if (GridAsked())
        {
            for (int i = 0; i < 50; i++)
                Slot(i, player.inventory, 0, i, chestStack: true);
            // The grid's gaps keep clicks off the world too.
            if (TrySlotPosition(0, 10, out Vector2 topLeft) && TrySlotPosition(0, 9, out Vector2 bottomRight) &&
                new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X) + 44, (int)(bottomRight.Y - topLeft.Y) + 44).Contains(Main.mouseX, Main.mouseY))
            {
                player.mouseInterface = true;
            }
        }
        if (CoinsAsked())
        {
            for (int i = 50; i < 58; i++)
                Slot(i, player.inventory, i < 54 ? 1 : 2, i);
        }
        if (TrashAsked())
        {
            _trash[0] = player.trashItem;
            Slot(58, _trash, 6, 0);
            player.trashItem = _trash[0];
        }
    }

    // The hotbar while the inventory is closed (Aether's UIHotbar): its slots (a click selects one, unless
    // the hotbar is locked or the held item busy), the held item's own slot when it's from further in the
    // inventory, and the held item's name above it, shown when the held item changes and then faded out.
    private void PresentHotbar()
    {
        if (!IsReady || !HotbarAsked())
        {
            _nameClock = 0;
            return;
        }
        Player player = Main.LocalPlayer;
        for (int i = 0; i < 10; i++)
        {
            if (!TrySlotPosition(13, i, out Vector2 position))
                continue;
            FreshUI.Legacy.Hud.Bottom = position.Y + Cell * 2;
            if (new Rectangle((int)position.X, (int)position.Y, (int)(Cell * 2), (int)(Cell * 2)).Contains(Main.mouseX, Main.mouseY) &&
                !player.hbLocked && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface && !player.controlTorch && !player.channel)
            {
                player.mouseInterface = true;
                player.cursorItemIconEnabled = false;
                if (Main.mouseLeft && !Main.blockMouse)
                    player.changeItem = i;
            }
            Present(_owners[60 + i], player.inventory, 13, i, position);
        }
        if ((player.selectedItemState.HasActiveOverride || player.selectedItem >= 10) && player.selectedItem != 58 && !player.HeldItem.IsAir &&
            TrySlotPosition(36, player.selectedItem, out Vector2 held))
        {
            Present(_owners[59], player.inventory, 36, player.selectedItem, held, interactive: false);
        }
        PresentHeldName(player);
    }

    private long _nameClock;
    private float _nameTicks;
    private int _nameSlot, _nameType, _namePrefix;
    private string? _name;
    private int _nameOfType, _nameOfPrefix;

    private void PresentHeldName(Player player)
    {
        Item selected = player.inventory[player.selectedItem];
        // Whatever is held when the hotbar appears (entering a world, closing the inventory) is taken as
        // is: only a change while it's shown shows the name.
        bool appearing = _nameClock == 0;
        float ticks = FreshUI.Legacy.Hud.TicksSince(ref _nameClock);
        if (appearing || ticks > FreshUI.Legacy.Hud.HotbarGap)
        {
            (_nameSlot, _nameType, _namePrefix) = (player.selectedItem, selected.type, selected.prefix);
            _nameTicks = FreshUI.Legacy.Hud.NameShown + FreshUI.Legacy.Hud.NameFade;
        }
        else if (player.selectedItem != _nameSlot || selected.type != _nameType || selected.prefix != _namePrefix)
        {
            (_nameSlot, _nameType, _namePrefix) = (player.selectedItem, selected.type, selected.prefix);
            _nameTicks = 0f;
        }
        else
        {
            _nameTicks += ticks;
        }
        float opacity = 1f - MathHelper.Clamp((_nameTicks - FreshUI.Legacy.Hud.NameShown) / FreshUI.Legacy.Hud.NameFade, 0f, 1f);
        if (opacity <= 0f)
            return;
        // The name with its prefix (AffixName builds it anew each call), kept while the item is the same.
        if (string.IsNullOrEmpty(selected.Name))
        {
            _name = Lang.inter[37].Value;
            _nameOfType = -1;
        }
        else if (_name == null || selected.type != _nameOfType || selected.prefix != _nameOfPrefix)
        {
            (_nameOfType, _nameOfPrefix) = (selected.type, selected.prefix);
            _name = selected.AffixName();
        }
        PresentName(_name, 0, FreshUI.Legacy.Hud.NameColor(selected) * opacity);
    }

    // One of the inventory's slots: the game's handling while the cursor's on it, then presented.
    private void Slot(int owner, Item[] items, int context, int index, bool chestStack = false)
    {
        if (!TrySlotPosition(context, context == 6 ? 0 : index, out Vector2 position))
            return;
        if (new Rectangle((int)position.X, (int)position.Y, (int)(Cell * 2), (int)(Cell * 2)).Contains(Main.mouseX, Main.mouseY) &&
            !Terraria.GameInput.PlayerInput.IgnoreMouseInterface)
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.inventoryScale = FreshItemSlot.DefaultScale;
            bool allow = true;
            if (chestStack)
            {
                // A slot filled from a chest's stack is marked until it's empty again (as Aether's grid).
                bool[] marked = Main.LocalPlayer.inventoryChestStack;
                if (marked[index] && items[index].IsAir)
                    marked[index] = false;
                allow = !marked[index];
            }
            ItemSlot.Handle(items, context, index, allow);
        }
        Present(_owners[owner], items, context, index, position);
    }

    private static FreshIconButtonLook LockLook(Texture2D texture) => new(texture,
        new Rectangle(0, 0, 24, texture.Height), texture, new Rectangle(26, 0, 24, texture.Height));

    private static Vector2 GroupOrigin => new((GumService.Default.CanvasWidth - GroupWidth) / 2,
        GumService.Default.CanvasHeight - BottomMargin - GroupHeight);

    /// <summary>Coordinates in Terraria UI units; both Gum art and native hit boxes use these.</summary>
    public static bool TrySlotPosition(int context, int index, out Vector2 position)
    {
        position = default;
        if (!IsReady)
            return false;
        (float Column, float Top) cell = context switch
        {
            0 when index < 10 => (index + 1, HotbarTop),
            0 when index < 50 => (index % 10 + 1, InventoryTop + (index / 10 - 1) * Pitch),
            1 => (0, InventoryTop + (index - 50) * Pitch),
            2 => (11, InventoryTop + (index - 54) * Pitch),
            6 => (11, HotbarTop),
            3 or 4 or 32 => (index % 10 + 1, (index / 10 - ChestUI.StartingRowForDrawing) * Pitch),
            15 => (index % 10 + 1, index / 10 * Pitch),
            13 => (index + 1, HotbarTop),
            36 => (11, HotbarTop),
            _ => (float.NaN, 0),
        };
        if (float.IsNaN(cell.Column))
            return false;
        position = (GroupOrigin + new Vector2(cell.Column * Pitch, cell.Top)) * 2;
        return true;
    }

    // At draw time, not in Gum's update: that runs before the game handles the key opening the inventory.
    private void PlaceHotbarLock()
    {
        FreshIconButton button = _hotbarLock;
        button.Visual.Visible = Main.playerInventory;
        button.IsChecked = Main.LocalPlayer.hbLocked;
        button.Visual.X = GroupOrigin.X + MathF.Round((Cell - button.Visual.Width) / 2);
        button.Visual.Y = GroupOrigin.Y + HotbarTop + MathF.Round((Cell - button.Visual.Height) / 2);
        // On the game's gamepad navigation (a page of its own): lit while it's there, A toggles it.
        bool focused = false;
        if (Main.playerInventory)
        {
            FreshUI.Legacy.InventoryNavigation.PlaceHotbarLock(new Vector2(button.Visual.X + button.Visual.Width / 2, button.Visual.Y + button.Visual.Height / 2) * 2);
            focused = FreshUI.Legacy.InventoryNavigation.OnHotbarLock;
            if (focused && Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                Main.LocalPlayer.hbLocked = !Main.LocalPlayer.hbLocked;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Unlock);
            }
        }
        button.IsLit = focused;
    }

    /// <summary>Drops the slots the last frame didn't present, and starts collecting this frame's.</summary>
    public static void BeginFrame()
    {
        GumHost.EnsureReady();
        if (_current == null)
            return;
        _current._namePresented = false;
        _current._overDraws.Clear();
        _current._icons.Clear();
        foreach ((object owner, SlotBinding slot) in _current._slots)
        {
            if (!slot.View.Visual.Visible)
            {
                _current.Children.Remove(slot.View.Visual);
                _current._expired.Add(owner);
            }
            slot.Presented = false;
        }
        foreach (object owner in _current._expired)
            _current._slots.Remove(owner);
        _current._expired.Clear();
    }

    /// <summary>
    /// Before Gum draws: hides what this frame didn't present. Visibility changes only when that
    /// changes, since each change has Gum lay the element out again.
    /// </summary>
    public static void EndFrame()
    {
        if (_current == null)
            return;
        _current.PresentInventory();
        _current.PresentHotbar();
        _current.PresentEquipment();
        _current.PresentMiscEquipment();
        _current.PresentHousing();
        _current.PresentContainers();
        _current.PresentChestControls();
        _current.PresentCrafting();
        _current._crafting.Present();
        _current.PresentShopInfo();
        _current.PresentNpcChat();
        _current._stylist.Present();
        _current._dresser.Present();
        _current.PlaceHotbarLock();
        if (!_current._namePresented && _current._heldName.Visible)
            _current._heldName.Visible = false;
        foreach (SlotBinding slot in _current._slots.Values)
        {
            if (!slot.Presented && slot.View.Visual.Visible)
                slot.View.Visual.Visible = false;
        }
    }

    /// <summary>
    /// Draws game art over the slots (an equipment slot's corner toggle, which the game draws before
    /// Gum draws the slots), at its position in UI units; false: Gum isn't ready, the game draws it.
    /// </summary>
    public static bool DrawOver(Texture2D texture, Vector2 position, Color color)
    {
        if (!IsReady)
            return false;
        _current!._overDraws.Add((texture, position, color));
        return true;
    }

    public static bool PresentName(string text, float top, Color color)
    {
        if (!IsReady)
            return false;
        TextRuntime label = _current!._heldName;
        // Gum's setters notify (and allocate) even for an unchanged value.
        if (label.Text != text)
            label.Text = text;
        float y = GroupOrigin.Y + HotbarTop - 12;
        if (label.Y != y)
            label.Y = y;
        if (label.Color != color)
            label.Color = color;
        if (!label.Visible)
            label.Visible = true;
        _current._namePresented = true;
        return true;
    }

    public static bool IsReady => GumHost.IsInitialized && _current is { Visible: true, Parent: not null };

    /// <summary>
    /// Called at Aether's slot draw sites, after its normal mouse handling. False keeps the native
    /// drawing as a fallback until Gum is ready. The owner is the retained slot, not its item array:
    /// trash and Guide slots use scratch arrays, and containers can change their array and index.
    /// </summary>
    public static bool Present(object owner, Item[] items, int context, int index, Vector2 position,
        Action<Item>? writeBack = null, Func<bool>? beforeHandle = null, bool interactive = true)
    {
        HudScreen? screen = _current;
        if (!IsReady || screen == null)
            return false;
        if (!screen._slots.TryGetValue(owner, out SlotBinding? binding))
        {
            binding = new SlotBinding();
            screen._slots.Add(owner, binding);
            // Under the game art drawn over the slots.
            screen.Children.Insert(screen.Children.IndexOf(screen._over), binding.View.Visual);
        }
        FreshItemSlot view = binding.View;
        if (view.Visual.Name == null || binding.Context != context || binding.Index != index)
            view.Visual.Name = $"{context}:{index}";
        binding.Items = items;
        binding.Context = context;
        binding.Index = index;
        binding.WriteBack = writeBack;
        binding.BeforeHandle = beforeHandle;
        binding.Presented = true;
        view.HudAction = interactive ? binding.Action : null;
        bool onGrid = TrySlotPosition(context, index, out Vector2 gridPosition);
        if (onGrid)
            position = gridPosition;
        float scale = onGrid ? FreshItemSlot.DefaultScale : Main.inventoryScale;
        if (view.Visual.X != position.X / 2)
            view.Visual.X = position.X / 2;
        if (view.Visual.Y != position.Y / 2)
            view.Visual.Y = position.Y / 2;
        if (!view.Visual.Visible)
            view.Visual.Visible = true;
        view.SetItem(items[index], context, index, scale);

        // ItemSlot.Draw normally publishes these positions as well as drawing the slot.
        int point = ItemSlot.GetGamepadPointForSlot(items, context, index);
        if (point != -1)
            UILinkPointNavigator.SetPosition(point, position + new Vector2(52 * scale * 0.75f));
        // The gamepad's slot shows it, as ItemSlot.Draw does.
        view.IsGamepadFocused = point != -1 && Terraria.GameInput.PlayerInput.UsingGamepadUI && UILinkPointNavigator.InUse && UILinkPointNavigator.CurrentPoint == point;
        return true;
    }

    private sealed class SlotBinding
    {
        public readonly FreshItemSlot View = new();
        public readonly Func<bool, bool> Action;
        public Item[] Items = [];
        public int Context;
        public int Index;
        public Action<Item>? WriteBack;
        public Func<bool>? BeforeHandle;
        public bool Presented;

        public SlotBinding()
        {
            Action = Act;
            // Physical mouse input stays with Aether's slot handlers, including held right-click and
            // shift-click. Automation calls Act directly, so one physical click never acts twice.
            View.Visual.HasEvents = false;
            View.HudAction = Action;
        }

        public bool Act(bool secondary)
        {
            Player player = Main.LocalPlayer;
            if (!View.Visual.Visible || Main.gameMenu || Main.ingameOptionsWindow || Main.inFancyUI ||
                Main.mapFullscreen || Main.hideUI || player.itemAnimation != 0 || player.itemTime != 0)
                return false;
            if (Context is 13 or 36)
            {
                if (secondary || player.hbLocked || player.controlTorch || player.channel || Main.blockMouse)
                    return false;
                player.changeItem = Index;
                return true;
            }
            if (!Main.playerInventory || BeforeHandle?.Invoke() == false)
                return false;
            bool left = Main.mouseLeft, leftRelease = Main.mouseLeftRelease;
            bool right = Main.mouseRight, rightRelease = Main.mouseRightRelease;
            try
            {
                Main.mouseLeft = Main.mouseLeftRelease = !secondary;
                Main.mouseRight = Main.mouseRightRelease = secondary;
                if (secondary)
                    ItemSlot.RightClick(Items, Context, Index);
                else
                    ItemSlot.LeftClick(Items, Context, Index);
                WriteBack?.Invoke(Items[Index]);
                View.SetItem(Items[Index], Context, Index, View.Scale);
                return true;
            }
            finally
            {
                Main.mouseLeft = left;
                Main.mouseLeftRelease = leftRelease;
                Main.mouseRight = right;
                Main.mouseRightRelease = rightRelease;
            }
        }
    }
}
