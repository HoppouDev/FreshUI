using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The equipment page (Aether's <see cref="UIEquipmentSlots"/>), on the art's pixel grid: armour and
/// accessories, their vanity and dyes in three columns and rows 46 apart (one art pixel between slots,
/// no gap before the accessories); each slot handled by the game's <c>ItemSlot.Handle</c> as the page's
/// were (a locked slot takes no item, <c>Main.armorHide</c> while one's used), the accessories' visibility
/// toggles at their rows' right, the loadout buttons, the defense counter and, where the inventory has no
/// room for it, the achievement card.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> EquipmentAsked = GumRoutes.GameScreen<UIEquipmentSlots>();

    // The page's top and row count, given to UIEquipmentSlots.Draw by the game; and the game's internals it uses.
    private static readonly FieldInfo EquipmentTop = typeof(UIEquipmentSlots).GetField("_top", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo EquipmentRows = typeof(UIEquipmentSlots).GetField("_slotCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly Action<int, bool, bool> DrawLoadoutButtons = typeof(Main).GetMethod("DrawLoadoutButtons", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Action<int, bool, bool>>();
    private static readonly Action<int, int> DrawDefenseCounter = typeof(Main).GetMethod("DrawDefenseCounter", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Action<int, int>>();
    private static readonly FieldInfo SettingsPushedAside = typeof(Main).GetField("_settingsButtonIsPushedToSide", BindingFlags.NonPublic | BindingFlags.Static)!;

    private const int EquipmentPitch = 46;

    private static int Even(float value) => (int)value - ((int)value & 1);

    private static int EquipmentColumn(int column) => Even(Main.screenWidth - 64 - 28 - EquipmentPitch * column);

    // The defense counter's and achievement card's places this frame, drawn in the HUD's game view.
    private Point? _defenseAt;
    private Vector2? _advisorAt;

    private void PresentEquipment()
    {
        _defenseAt = null;
        _advisorAt = null;
        if (!IsReady || !EquipmentAsked())
            return;
        Player player = Main.LocalPlayer;
        int top = (int)EquipmentTop.GetValue(UIEquipmentSlots.Instance)!;
        int rows = (int)EquipmentRows.GetValue(UIEquipmentSlots.Instance)!;
        bool demonHeart = player.CanDemonHeartAccessoryBeShown();
        bool masterMode = player.CanMasterModeAccessoryBeShown();
        int defenseRow = rows - 1 - ((bool)SettingsPushedAside.GetValue(null)! ? 1 : 0);
        int advisorRow = Main.HasSpaceForControllerAchievementAdvisor ? defenseRow - 1 : -1;
        DrawLoadoutButtons(top, demonHeart, masterMode);
        float saved = Main.inventoryScale;
        Main.inventoryScale = Legacy.Hud.SlotScale;
        int row = -1;
        for (int i = 0; i < 10; i++)
        {
            int accessory = i % 10;
            if (accessory == 8 && !demonHeart || accessory == 9 && !masterMode)
                continue;
            row++;
            int y = Even(top + row * EquipmentPitch);
            EquipmentSlot(70 + i, player.armor, i, i > 2 ? 10 : 8, EquipmentColumn(0), y);
            EquipmentSlot(80 + i, player.armor, 10 + i, i > 2 ? 11 : 9, EquipmentColumn(1), y);
            EquipmentSlot(90 + i, player.dye, i, 12, EquipmentColumn(2), y);
            if (i > 2)
                AccessoryToggle(player, i, new Vector2(Even(Main.screenWidth - 58), y - 2));
            if (row == advisorRow && !Main.AchievementAdvisor.CanDrawAboveCoins)
                _advisorAt = new Vector2(EquipmentColumn(0) - 10 - EquipmentPitch * 2 - 28, y + 8);
            if (row == defenseRow)
                _defenseAt = new Point(EquipmentColumn(0), y);
        }
        Main.inventoryScale = saved;
        // The columns' areas, gaps included, keep clicks off the world.
        if (!PlayerInput.IgnoreMouseInterface &&
            (new Rectangle(EquipmentColumn(0), top, 48, 448 * 85 / 100).Contains(Main.mouseX, Main.mouseY) ||
             new Rectangle(EquipmentColumn(2), top, EquipmentPitch * 2 + 48, 168 * 85 / 100).Contains(Main.mouseX, Main.mouseY)))
        {
            player.mouseInterface = true;
        }
    }

    // An equipment slot: the game's handling while the cursor's on it (a locked one takes no item), then presented.
    private void EquipmentSlot(int owner, Item[] items, int index, int context, int x, int y)
    {
        var position = new Vector2(x, y);
        if (new Rectangle(x, y, (int)(Cell * 2), (int)(Cell * 2)).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            Player player = Main.LocalPlayer;
            player.mouseInterface = true;
            Main.armorHide = true;
            bool usable = player.IsItemSlotUnlockedAndUsable(context == 12 ? index : index) || Main.mouseItem.IsAir;
            ItemSlot.Handle(items, context, index, usable);
        }
        Present(_owners[owner], items, context, index, position);
    }

    // An accessory's visibility toggle (shown while nothing's held): a click flips it, its state in the hover text.
    private void AccessoryToggle(Player player, int accessory, Vector2 position)
    {
        if (!Main.mouseItem.IsAir)
            return;
        Texture2D texture = player.hideVisibleAccessory[accessory] ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value;
        if (new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            player.mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                player.hideVisibleAccessory[accessory] = !player.hideVisibleAccessory[accessory];
                Main.mouseLeftRelease = false;
                SoundEngine.PlaySound(12);
                if (Main.netMode == 1)
                    NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
            }
            Main.ClearHoverItem();
            Main.hoverItemName = Lang.inter[58 + (player.hideVisibleAccessory[accessory] ? 2 : 1)].Value;
        }
        DrawOver(texture, position, Color.White * 0.7f);
    }

    // In the HUD's game view, over the slots: the defense counter and the achievement card.
    private void DrawEquipmentExtras(SpriteBatch batch)
    {
        Legacy.Sharp.WholePixels(batch, true);
        if (_defenseAt is Point defense)
            DrawDefenseCounter(defense.X, defense.Y);
        if (_advisorAt is Vector2 advisor)
        {
            Main.AchievementAdvisor.DrawOneAchievement(batch, advisor, large: false);
            Terraria.UI.Gamepad.UILinkPointNavigator.SetPosition(1570, advisor + new Vector2(20f) * Main.inventoryScale);
        }
        Legacy.Sharp.WholePixels(batch, false);
    }
}
