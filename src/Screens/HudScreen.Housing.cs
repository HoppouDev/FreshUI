using System.Reflection;
using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The NPC housing page (Aether's <see cref="UIHousingPage"/>): the housing query and one head per town
/// NPC type present, in slots down the equipment page's first column on the art's grid (46 apart), a new
/// column to the left once one reaches the screen's bottom; hovering one names it, clicking picks it up to
/// assign a house; the gamepad's points and housing cursor as the game's.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> HousingAsked = GumRoutes.GameScreen<UIHousingPage>();
    private static readonly FieldInfo HousingTop = typeof(UIHousingPage).GetField("_top", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo HidePvpIcons = typeof(Main).GetField("hidePVPAndTeamIcons", BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly List<FreshItemSlot> _housing = [];
    private readonly int[] _npcByHead = new int[NPCHeadID.Count];
    private readonly List<int> _typesWithHead = [];
    private int _housingShown;

    private void PresentHousing()
    {
        int shown = 0;
        if (IsReady && HousingAsked())
            shown = HousingIcons();
        for (int i = shown; i < _housingShown; i++)
            _housing[i].Visual.Visible = false;
        _housingShown = shown;
    }

    private int HousingIcons()
    {
        Player player = Main.LocalPlayer;
        int top = (int)HousingTop.GetValue(UIHousingPage.Instance)!;
        int columnX = Even(Main.screenWidth - 64 - 28);
        // One head per town NPC type: the first NPC of each type with a head the page can show.
        _typesWithHead.Clear();
        Array.Fill(_npcByHead, -1);
        for (int j = 0; j < Main.maxNPCs; j++)
        {
            NPC npc = Main.npc[j];
            if (!npc.active || _typesWithHead.Contains(npc.type))
                continue;
            int head = TownNPCProfiles.GetHeadIndexSafe(npc);
            if (head > 0 && head <= NPCHeadID.Count && !NPCHeadID.Sets.CannotBeDrawnInHousingUI[head] && _npcByHead[head] == -1)
            {
                _npcByHead[head] = j;
                _typesWithHead.Add(npc.type);
            }
        }
        HidePvpIcons.SetValue(null, false);
        UILinkPointNavigator.Shortcuts.NPCS_IconsPerColumn = 1;
        UILinkPointNavigator.Shortcuts.NPCS_SelectedNPC = -2;
        string hover = "";
        int index = 0, row = 0, column = 0, inColumn = 0;
        foreach (int head in NPCHeadID.Sets.HeadListOrder)
        {
            if (head != 0 && _npcByHead[head] == -1)
                continue;
            int y = Even(top + row * EquipmentPitch);
            if (y > Main.screenHeight - 80)
            {
                column++;
                row = 0;
                y = Even(top);
                UILinkPointNavigator.Shortcuts.NPCS_IconsPerColumn = inColumn;
                if (column >= 3)
                    HidePvpIcons.SetValue(null, true);
                inColumn = 0;
            }
            var position = new Vector2(columnX - column * EquipmentPitch, y);
            int npc = head != 0 ? _npcByHead[head] : -1;
            if (HousingIcon(index, head, npc, position, player))
                hover = head != 0 ? Main.npc[npc].FullName : Lang.inter[8].Value;
            index++;
            row++;
            inColumn++;
        }
        if (UILinkPointNavigator.Shortcuts.NPCS_IconsPerColumn < inColumn)
            UILinkPointNavigator.Shortcuts.NPCS_IconsPerColumn = inColumn;
        UILinkPointNavigator.Shortcuts.NPCS_IconsTotal = index;
        // The equipment page's first column keeps clicks off the world.
        if (!PlayerInput.IgnoreMouseInterface && new Rectangle(columnX, top, 48, 448 * 85 / 100).Contains(Main.mouseX, Main.mouseY))
            player.mouseInterface = true;
        if (hover != "" && Main.mouseItem.type == 0)
            Main.instance.MouseText(hover, 0, 0);
        PlayerInput.UpdateHousingCursor();
        return index;
    }

    // One icon: hovered (edges included), a click picks the NPC (or the housing query) up; its slot's
    // background the gamepad's selected one while the gamepad's on it. True while hovered.
    private bool HousingIcon(int index, int head, int npc, Vector2 position, Player player)
    {
        if (index >= _housing.Count)
        {
            var created = new FreshItemSlot();
            created.Visual.HasEvents = false;
            _housing.Add(created);
            Children.Insert(Children.IndexOf(_over), created.Visual);
        }
        FreshItemSlot view = _housing[index];
        float size = FreshItemSlot.SizeAt(Hooks.Hud.SlotScale) * 2;
        bool hovered = Main.mouseX >= position.X && Main.mouseX <= position.X + size && Main.mouseY >= position.Y && Main.mouseY <= position.Y + size;
        if (hovered)
        {
            Main.mouseText = true;
            UILinkPointNavigator.Shortcuts.NPCS_SelectedNPC = npc;
            if (!PlayerInput.IgnoreMouseInterface)
            {
                player.mouseInterface = true;
                if (Main.mouseLeftRelease && Main.mouseLeft && !PlayerInput.UsingGamepadUI && Main.mouseItem.type == 0)
                {
                    SoundEngine.PlaySound(12);
                    if (head == 0)
                        Main.instance.SetMouseNPC_ToHousingQuery();
                    else
                        Main.instance.SetMouseNPC(npc, Main.npc[npc].type);
                    Main.mouseLeftRelease = false;
                }
            }
        }
        UILinkPointNavigator.SetPosition(600 + index, position + TextureAssets.InventoryBack.Size() * 0.75f);
        bool selected = UILinkPointNavigator.CurrentPoint - 600 == index;
        if (view.Scale != Hooks.Hud.SlotScale)
            view.Scale = Hooks.Hud.SlotScale;
        string background = selected ? "Images/Inventory_Back14" : "Images/Inventory_Back11";
        if (view.BackgroundPath != background)
            view.BackgroundPath = background;
        var texture = TextureAssets.NpcHead[head].Value;
        if (view.Item != texture)
            view.Item = texture;
        if (view.Visual.X != position.X / 2 || view.Visual.Y != position.Y / 2)
        {
            view.Visual.X = position.X / 2;
            view.Visual.Y = position.Y / 2;
        }
        if (!view.Visual.Visible)
            view.Visual.Visible = true;
        return hovered;
    }
}
