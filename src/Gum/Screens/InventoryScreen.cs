using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.GumUi.Screens;

/// <summary>What the inventory's container rows hold, which picks their slots' background.</summary>
public enum InventoryContainer
{
    Chest,
    PiggyBank,
    Safe,
    DefendersForge,
    VoidVault,
}

/// <summary>
/// A sample inventory at the bottom centre: an open container's four rows (a chest's, a piggy bank's)
/// above the inventory's four, with coins in a column on the inventory's left and ammo in one on its
/// right, and the hotbar row below, between its lock (under the coins) and the trash (under the ammo).
/// The container's rows show only while <see cref="IsContainerOpen"/>, in its <see cref="Container"/>'s
/// background.
/// </summary>
public sealed class InventoryScreen : ContainerRuntime
{
    // UI pixels from one slot to the next: a 22-pixel slot and a 1-pixel gap.
    private const float Pitch = 23;
    private const int Columns = 12, Rows = 5, ContainerRows = 4;

    // The container's rows, then the inventory's, at the same pitch.
    private const float InventoryTop = ContainerRows * Pitch;

    // A chest's slots are red-brown; the player's own storage (the banks) pink-purple.
    private static readonly Dictionary<InventoryContainer, string> ContainerBackgrounds = new()
    {
        [InventoryContainer.Chest] = "Images/Inventory_Back5",
        [InventoryContainer.PiggyBank] = "Images/Inventory_Back2",
        [InventoryContainer.Safe] = "Images/Inventory_Back2",
        [InventoryContainer.DefendersForge] = "Images/Inventory_Back2",
        [InventoryContainer.VoidVault] = "Images/Inventory_Back2",
    };

    // The lock's highlight colour, for its outline while hovered.
    private static readonly Color LockHighlight = new(255, 231, 69);

    private static readonly (int Id, int Count)[] HotbarItems =
    [
        (3507, 1), (1, 1), (3509, 1), (4, 1), (280, 1), (166, 32), (8, 158), (9, 130), (2, 125), (28, 15),
    ];

    private static readonly (int Id, int Count)[] Items =
    [
        (29, 3), (188, 5), (189, 22), (22, 976), (3, 301), (117, 40), (23, 232), (31, 6), (39, 1), (965, 409),
    ];

    private static readonly (int Id, int Count)[] ContainerItems =
    [
        (9, 999), (3, 999), (2, 742), (8, 99), (22, 45), (23, 120), (28, 30), (188, 12), (40, 999), (965, 250),
        (1, 1), (4, 1), (117, 15),
    ];

    private readonly List<FreshItemSlot> _container = [];
    private readonly List<FreshItemSlot> _hotbar = [];
    private readonly FreshIconButton _hotbarLock;
    private bool _isContainerOpen = true;
    private InventoryContainer _containerKind;

    public InventoryScreen()
    {
        float gap = Pitch - FreshItemSlot.SizeAt(FreshItemSlot.DefaultScale);
        Width = Columns * Pitch - gap;
        Height = InventoryTop + Rows * Pitch - gap;
        Anchor(Gum.Wireframe.Anchor.Bottom);
        Y = -10;

        // The container, over the inventory's ten columns.
        for (int row = 0; row < ContainerRows; row++)
        {
            for (int column = 0; column < 10; column++)
            {
                FreshItemSlot slot = Add(column + 1, row, FreshItemSlotKind.Container, top: 0);
                slot.BackgroundPath = ContainerBackgrounds[_containerKind];
                _container.Add(slot);
                int index = row * 10 + column;
                if (index < ContainerItems.Length)
                {
                    slot.Item = LoadItem(ContainerItems[index].Id);
                    slot.Count = ContainerItems[index].Count;
                }
            }
        }

        // The inventory: the middle ten columns of its first four rows.
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 10; column++)
            {
                FreshItemSlot slot = Add(column + 1, row, FreshItemSlotKind.Inventory);
                if (row == 0)
                {
                    slot.Item = LoadItem(Items[column].Id);
                    slot.Count = Items[column].Count;
                }
            }
        }

        // Coins down the first column, ammo down the last.
        for (int row = 0; row < 4; row++)
        {
            FreshItemSlot coin = Add(0, row, FreshItemSlotKind.Coin);
            coin.Item = LoadItem(71 + row);
            coin.Count = 38 - row * 9;

            FreshItemSlot ammo = Add(Columns - 1, row, FreshItemSlotKind.Ammo);
            if (row == 0)
            {
                ammo.Item = LoadItem(40);
                ammo.Count = 5439;
            }
        }

        // The hotbar below the inventory. While it's locked, clicking its slots doesn't select them.
        for (int column = 0; column < 10; column++)
        {
            FreshItemSlot slot = Add(column + 1, Rows - 1, FreshItemSlotKind.Hotbar);
            slot.Item = LoadItem(HotbarItems[column].Id);
            slot.Count = HotbarItems[column].Count;
            slot.HotbarIndex = column;
            slot.IsHighlighted = column == 0;
            _hotbar.Add(slot);
            slot.Click += (_, _) =>
            {
                if (!IsHotbarLocked)
                {
                    _hotbar.ForEach(other => other.IsHighlighted = other == slot);
                }
            };
        }

        // Its lock, under the coins: a toggle, closed and gold while locked, open and grey while not,
        // outlined in the highlight colour while hovered, centred in its cell on the UI's grid.
        _hotbarLock = new FreshIconButton
        {
            IsToggle = true,
            Look = LockLook(Load("Images/Lock_1")),
            CheckedLook = LockLook(Load("Images/Lock_0")),
            HoverOverlayColor = LockHighlight,
        };
        float cell = FreshItemSlot.SizeAt(FreshItemSlot.DefaultScale);
        _hotbarLock.Visual.X = MathF.Round((cell - _hotbarLock.Visual.Width) / 2);
        _hotbarLock.Visual.Y = InventoryTop + (Rows - 1) * Pitch + MathF.Round((cell - _hotbarLock.Visual.Height) / 2);
        Children.Add(_hotbarLock.Visual);
        FreshTooltip.Attach(_hotbarLock.Visual, () => IsHotbarLocked ? "Hotbar locked" : "Hotbar unlocked");

        FreshItemSlot trash = Add(Columns - 1, Rows - 1, FreshItemSlotKind.Trash);
        trash.Placeholder = Load("Images/Trash");
        FreshTooltip.Attach(trash.Visual, () => "Trash Can");
    }

    /// <summary>
    /// Whether a container (a chest, a piggy bank) is open: its slots show above the inventory and can
    /// be clicked. The inventory stays where it is either way.
    /// </summary>
    public bool IsContainerOpen
    {
        get => _isContainerOpen;
        set
        {
            _isContainerOpen = value;
            foreach (FreshItemSlot slot in _container)
            {
                slot.Visual.Visible = value;
            }
        }
    }

    /// <summary>The container whose slots show (while <see cref="IsContainerOpen"/>), which picks their background.</summary>
    public InventoryContainer Container
    {
        get => _containerKind;
        set
        {
            _containerKind = value;
            foreach (FreshItemSlot slot in _container)
            {
                slot.BackgroundPath = ContainerBackgrounds[value];
            }
        }
    }

    /// <summary>Shows <paramref name="container"/>'s slots above the inventory.</summary>
    public void OpenContainer(InventoryContainer container)
    {
        Container = container;
        IsContainerOpen = true;
    }

    /// <summary>Hides the container's slots.</summary>
    public void CloseContainer() => IsContainerOpen = false;

    /// <summary>Whether the hotbar is locked: clicking its slots doesn't select them.</summary>
    public bool IsHotbarLocked
    {
        get => _hotbarLock.IsChecked;
        set => _hotbarLock.IsChecked = value;
    }

    // A lock texture holds two 25-texel frames: the icon (its last column empty, left off so it's
    // whole pixels wide) and, from texel 26 so it shares the icon's pixel grid, its outline, which
    // surrounds the icon by a pixel.
    private static FreshIconButtonLook LockLook(Texture2D texture) => new(
        texture,
        new Rectangle(0, 0, 24, texture.Height),
        texture,
        new Rectangle(26, 0, 24, texture.Height));

    // A slot in a grid whose first row is at top (the inventory's, unless given).
    private FreshItemSlot Add(int column, int row, FreshItemSlotKind kind, float top = InventoryTop)
    {
        var slot = new FreshItemSlot { Kind = kind };
        slot.Visual.X = column * Pitch;
        slot.Visual.Y = top + row * Pitch;
        Children.Add(slot.Visual);
        return slot;
    }

    private static Texture2D LoadItem(int id) => Load($"Images/Item_{id}");

    private static Texture2D Load(string path) => GumTextures.Load(path);
}
