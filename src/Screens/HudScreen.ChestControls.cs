using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using ReLogic.Localization.IME;
using ReLogic.OS;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The open chest's name over its slots (with the text being typed and a caret while renaming it) and its
/// buttons in a column beside them (Aether's <see cref="UIChestControls"/>: Loot All, Deposit All, Quick
/// Stack, Restock, Sort, Rename / Save and Cancel), each calling <see cref="ChestUI"/>'s action as Aether's did.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> ChestControlsAsked = GumRoutes.GameScreen<UIChestControls>();
    // Each button's gamepad point (Rename and Cancel share one), as the game's.
    private static readonly int[] ChestLinkPoints = [500, 501, 502, 503, 505, 504, 504];
    private const float ChestButtonPitch = 13;

    private TextRuntime? _chestName;
    private FreshValueButton[]? _chestButtons;

    private void PresentChestControls()
    {
        bool shown = IsReady && ChestControlsAsked();
        if (_chestName == null)
        {
            if (!shown)
                return;
            CreateChestControls();
        }
        if (_chestName!.Visible != shown)
            _chestName.Visible = shown;
        if (!shown)
        {
            foreach (FreshValueButton button in _chestButtons!)
            {
                if (button.Visual.Visible)
                    button.Visual.Visible = false;
            }
            return;
        }
        Player player = Main.LocalPlayer;
        float left = GroupOrigin.X + Pitch, top = GroupOrigin.Y;
        SetText(_chestName, ChestNameText());
        if (_chestName.X != left || _chestName.Y != top - 13)
        {
            _chestName.X = left;
            _chestName.Y = top - 13;
        }
        if (Main.editChest)
            Main.instance.SetIMEPanelAnchor(new Vector2(left + _chestName.GetAbsoluteWidth(), top) * 2, 0f);

        bool gamepad = PlayerInput.UsingGamepadUI && UILinkPointNavigator.InUse;
        float x = GroupOrigin.X + 11 * Pitch + 2;
        for (int id = 0; id < _chestButtons!.Length; id++)
        {
            FreshValueButton button = _chestButtons[id];
            bool visible = id switch { 5 => player.chest >= -1, 6 => Main.editChest, _ => true };
            if (button.Visual.Visible != visible)
                button.Visual.Visible = visible;
            if (!visible)
            {
                ChestUI.UpdateHover(id, hovering: false);
                continue;
            }
            SetText(button, ChestButtonText(id));
            float y = top + id * ChestButtonPitch;
            if (button.Visual.X != x || button.Visual.Y != y)
            {
                button.Visual.X = x;
                button.Visual.Y = y;
            }
            var area = new Rectangle((int)(x * 2), (int)(y * 2), (int)(button.Visual.GetAbsoluteWidth() * 2), (int)(ChestButtonPitch * 2));
            bool hovered = area.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;
            UILinkPointNavigator.SetPosition(ChestLinkPoints[id], area.Center.ToVector2());
            // The gamepad's focus is the game's (its points), not Gum's: A on a button acts.
            bool focused = gamepad && UILinkPointNavigator.CurrentPoint == ChestLinkPoints[id];
            button.IsLit = focused;
            ChestUI.UpdateHover(id, hovered);
            if (focused && Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                ChestAction(id);
            }
        }
    }

    private void CreateChestControls()
    {
        _chestName = new TextRuntime
        {
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 9.5f,
            Color = Color.White,
            HasEvents = false,
        };
        GumText.SetOutline(_chestName, 0.5f, Color.Black);
        Children.Add(_chestName);
        _chestButtons = new FreshValueButton[ChestUI.ButtonID.Count];
        for (int id = 0; id < _chestButtons.Length; id++)
        {
            int action = id;
            var button = new FreshValueButton { Color = Color.White };
            button.Visual.Height = ChestButtonPitch;
            // The mouse (and automation) clicks through Gum; the gamepad through the game's cursor.
            button.Click += (_, _) =>
            {
                if (!PlayerInput.UsingGamepadUI)
                    ChestAction(action);
            };
            _chestButtons[id] = button;
            Children.Insert(Children.IndexOf(_over), button.Visual);
        }
    }

    private static void ChestAction(int id)
    {
        if (!Main.playerInventory || Main.LocalPlayer.chest == -1)
            return;
        switch (id)
        {
            case 0: ChestUI.LootAll(); break;
            case 1: ChestUI.DepositAll(); break;
            case 2: ChestUI.QuickStack(); break;
            case 3: ChestUI.Restock(); break;
            case 4: ItemSorting.SortChest(); break;
            case 5:
                bool starting = !Main.editChest;
                ChestUI.RenameChest();
                if (starting && Main.editChest && !PlayerInput.UsingGamepad)
                    TypeInPlace();
                break;
            default: ChestUI.RenameChestCancel(); break;
        }
    }

    private static readonly System.Reflection.FieldInfo KeyboardInstance =
        typeof(Terraria.GameContent.UI.States.UIVirtualKeyboard).GetField("_currentInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    // The game renames in its on-screen keyboard; with a keyboard, the name is typed where it's shown instead
    // (the game's own typing for a rename while no on-screen keyboard is up, Enter saving and Esc cancelling).
    private static void TypeInPlace()
    {
        if (Main.InGameUI.CurrentState is Terraria.GameContent.UI.States.UIVirtualKeyboard)
            IngameFancyUI.Close(quiet: true);
        KeyboardInstance.SetValue(null, null);
    }

    private static void SetText(TextRuntime text, string value)
    {
        if (text.Text != value)
            text.Text = value;
    }

    private static void SetText(FreshValueButton button, string value)
    {
        if (button.Text != value)
            button.Text = value;
    }

    private static string ChestButtonText(int id) => id switch
    {
        0 => Lang.inter[29].Value,
        1 => Lang.inter[30].Value,
        2 => Lang.inter[31].Value,
        3 => Lang.inter[82].Value,
        4 => Lang.inter[122].Value,
        5 => Lang.inter[Main.editChest ? 47 : 61].Value,
        _ => Lang.inter[63].Value,
    };

    private string _typedName = "", _typedComposition = "", _typedText = "";
    private bool _typedCaret;

    // The container's name; while renaming, the text typed so far, the IME's composition and a blinking caret.
    private string ChestNameText()
    {
        if (!Main.editChest)
            return ContainerName();
        if (++Main.instance.textBlinkerCount >= 20)
        {
            Main.instance.textBlinkerState = Main.instance.textBlinkerState == 0 ? 1 : 0;
            Main.instance.textBlinkerCount = 0;
        }
        string typed = Main.npcChatText ?? "", composition = Platform.Get<IImeService>().CompositionString ?? "";
        bool caret = Main.instance.textBlinkerState == 1;
        if (typed != _typedName || composition != _typedComposition || caret != _typedCaret)
        {
            (_typedName, _typedComposition, _typedCaret) = (typed, composition, caret);
            _typedText = typed + composition + (caret ? "|" : "");
        }
        return _typedText;
    }

    private static string ContainerName()
    {
        Player player = Main.LocalPlayer;
        switch (player.chest)
        {
            case -2: return Lang.inter[32].Value;
            case -3: return Lang.inter[33].Value;
            case -4: return Lang.GetItemNameValue(3813);
            case -5: return Lang.GetItemNameValue(4076);
        }
        Chest? chest = player.chest > -1 ? Main.chest[player.chest] : null;
        if (chest == null)
            return "";
        if (chest.name != "")
            return chest.name;
        Tile tile = Main.tile[player.chestX, player.chestY];
        return tile.type switch
        {
            21 => Lang.chestType[tile.frameX / 36].Value,
            467 => tile.frameX / 36 == 4 ? Lang.GetItemNameValue(3988) : Lang.chestType2[tile.frameX / 36].Value,
            88 => Lang.dresserType[tile.frameX / 54].Value,
            _ => "",
        };
    }
}
