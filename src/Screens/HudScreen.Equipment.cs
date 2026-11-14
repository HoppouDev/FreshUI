using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
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
    private void EquipmentSlot(int owner, Item[] items, int index, int context, int x, int y, bool alwaysUsable = false)
    {
        var position = new Vector2(x, y);
        if (new Rectangle(x, y, (int)(Cell * 2), (int)(Cell * 2)).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            Player player = Main.LocalPlayer;
            player.mouseInterface = true;
            Main.armorHide = true;
            bool usable = alwaysUsable || player.IsItemSlotUnlockedAndUsable(index) || Main.mouseItem.IsAir;
            ItemSlot.Handle(items, context, index, usable);
        }
        Present(_owners[owner], items, context, index, position);
    }

    // An accessory's visibility toggle.
    private void AccessoryToggle(Player player, int accessory, Vector2 position)
    {
        bool hidden = player.hideVisibleAccessory[accessory];
        if (CornerToggle(hidden ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value, !hidden, supercart: false, Color.White * 0.7f, position))
        {
            player.hideVisibleAccessory[accessory] = !hidden;
            SyncPlayer();
        }
    }

    // A slot's corner toggle (Aether's UIEquipToggle), shown while nothing's held: true when clicked (the
    // caller flips it; synced in multiplayer), its state (1 on, 2 off) named in the hover text (the
    // visibility toggles' words, or the minecart upgrade's).
    private bool CornerToggle(Texture2D texture, bool isOn, bool supercart, Color tint, Vector2 position)
    {
        if (!Main.mouseItem.IsAir)
            return false;
        bool clicked = false;
        Player player = Main.LocalPlayer;
        if (new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height).Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            player.mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                clicked = true;
                Main.mouseLeftRelease = false;
                SoundEngine.PlaySound(12);
            }
            Main.ClearHoverItem();
            Main.hoverItemName = supercart ? Language.GetTextValue(isOn ? "GameUI.SuperCartDisabled" : "GameUI.SuperCartEnabled") : Lang.inter[isOn ? 59 : 60].Value;
        }
        DrawOver(texture, position, tint);
        return clicked;
    }

    // After a toggle's flipped: the player's state to the server.
    private static void SyncPlayer()
    {
        if (Main.netMode == 1)
            NetMessage.SendData(4, -1, -1, null, Main.myPlayer);
    }

    private static readonly Func<bool> MiscAsked = GumRoutes.GameScreen<UIMiscEquipSlots>();
    private static readonly FieldInfo MiscTop = typeof(UIMiscEquipSlots).GetField("_top", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly int[] MiscContexts = [19, 20, 18, 17, 16];

    // The misc equipment page (Aether's UIMiscEquipSlots): pet, light pet, minecart, mount and hook with
    // their dyes in two columns on the art's grid, the pet and light visibility toggles and, once unlocked,
    // the minecart upgrade's.
    private void PresentMiscEquipment()
    {
        if (!IsReady || !MiscAsked())
            return;
        Player player = Main.LocalPlayer;
        int top = (int)MiscTop.GetValue(UIMiscEquipSlots.Instance)!;
        float saved = Main.inventoryScale;
        Main.inventoryScale = Legacy.Hud.SlotScale;
        for (int i = 0; i < 5; i++)
        {
            int y = Even(top + i * EquipmentPitch);
            int x = Even(Main.screenWidth - 92);
            EquipmentSlot(100 + i, player.miscEquips, i, MiscContexts[i], x, y, alwaysUsable: true);
            EquipmentSlot(105 + i, player.miscDyes, i, 33, Even(Main.screenWidth - 92 - EquipmentPitch), y, alwaysUsable: true);
            var tick = new Vector2(x + 34, y - 2);
            if (i < 2)
            {
                if (CornerToggle(player.hideMisc[i] ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value, !player.hideMisc[i], supercart: false, Color.White * 0.7f, tick))
                {
                    if (i == 0)
                        player.TogglePet();
                    else
                        player.ToggleLight();
                    SyncPlayer();
                }
            }
            else if (i == 2 && player.unlockedSuperCart)
            {
                // "On" here means the upgrade is off, as in the game.
                if (CornerToggle(TextureAssets.Extra[player.enabledSuperCart ? 255 : 256].Value, !player.enabledSuperCart, supercart: true, Color.White, tick))
                {
                    player.enabledSuperCart = !player.enabledSuperCart;
                    SyncPlayer();
                }
            }
        }
        Main.inventoryScale = saved;
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
