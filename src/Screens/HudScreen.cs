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
public sealed class HudScreen : ContainerRuntime
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
        FreshTooltip.Attach(_hotbarLock.Visual, () => Main.LocalPlayer.hbLocked ? "Hotbar locked" : "Hotbar unlocked");
        _current = this;
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

    /// <summary>Runs after Aether's layout, before its UserInterface.Update performs hit testing.</summary>
    public static void LayoutSlots(UIGameScreen screen)
    {
        if (!IsReady)
            return;
        int hotbarIndex = 0;
        foreach (UIElement element in screen.Children)
        {
            if (element is UIGameItemSlot slot && TrySlotPosition(slot.Context, slot.Slot, out Vector2 position))
            {
                slot.Place(position, FreshItemSlot.DefaultScale);
                slot.Width.Set(Cell * 2, 0);
                slot.Height.Set(Cell * 2, 0);
                slot.Recalculate();
            }
            else if (element is UIHotbarSlot && TrySlotPosition(13, hotbarIndex++, out position))
            {
                element.Left.Set(position.X, 0);
                element.Top.Set(position.Y, 0);
                element.Width.Set(Cell * 2, 0);
                element.Height.Set(Cell * 2, 0);
                element.Recalculate();
                FreshUI.Legacy.Hud.Bottom = position.Y + Cell * 2;
            }
        }
        if (screen is UIChestSlots)
            ChestUI.LastChestDisplayRectangle = new Rectangle((int)((GroupOrigin.X + Pitch) * 2),
                (int)(GroupOrigin.Y * 2), (int)((10 * Pitch - 1) * 2), (int)((4 * Pitch - 1) * 2));
    }

    internal static void Update()
    {
        if (_current == null)
            return;
        FreshIconButton button = _current._hotbarLock;
        button.Visual.Visible = Main.playerInventory;
        button.IsChecked = Main.LocalPlayer.hbLocked;
        button.Visual.X = GroupOrigin.X + MathF.Round((Cell - button.Visual.Width) / 2);
        button.Visual.Y = GroupOrigin.Y + HotbarTop + MathF.Round((Cell - button.Visual.Height) / 2);
    }

    /// <summary>Drops the slots the last frame didn't present, and starts collecting this frame's.</summary>
    public static void BeginFrame()
    {
        if (_current == null)
            return;
        _current._namePresented = false;
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
        if (!_current._namePresented && _current._heldName.Visible)
            _current._heldName.Visible = false;
        foreach (SlotBinding slot in _current._slots.Values)
        {
            if (!slot.Presented && slot.View.Visual.Visible)
                slot.View.Visual.Visible = false;
        }
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
            screen.Children.Add(binding.View.Visual);
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
