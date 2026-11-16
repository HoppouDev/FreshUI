using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;

namespace FreshUI.Screens;

/// <summary>
/// Aether's icon buttons in Gum (<see cref="GumRoutes.GameScreens"/>): every single-button screen and
/// button group the game asks for (the inventory's side buttons, the builder toggles, the equipment page
/// icons, PvP and team icons, loadouts, the minimap's buttons) is drawn and handled here, in Gum's draw
/// order, instead of by the game: its own update (a group lays out its buttons), then each button as
/// Aether's <see cref="UIIconButton"/> did: hovered while the game's cursor (the mouse, or the gamepad's)
/// is on it, it claims the mouse, ticks, acts on a press and shows its tooltip; its art (the game's, its own
/// draw, which also places its gamepad point) at whole screen pixels per art pixel.
/// </summary>
sealed class GameIconButtons : FreshGameView
{
    private readonly List<UIGameScreen> _asked = [];
    private readonly HashSet<UIIconButton> _hovered = [];
    private readonly HashSet<UIGameScreen> _laidOut = [];

    public GameIconButtons()
    {
        WidthUnits = HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        GumRoutes.GameScreens(screen => screen is UIIconButtonScreen or UIIconButtonGroup && screen != InventoryButtons.HotbarLock, _asked.Add);
        Draw = (batch, _) => DrawAsked(batch);
    }

    /// <summary>Forgets what the game asked for (a frame without the HUD's draw).</summary>
    public void Clear() => _asked.Clear();

    private void DrawAsked(SpriteBatch batch)
    {
        foreach (UIGameScreen screen in _asked)
        {
            if (_laidOut.Add(screen))
                LayOut(screen);
            screen.UpdateState();
            if (screen is UIIconButtonScreen single)
                Button(batch, single.Button);
            else if (screen is UIIconButtonGroup group)
            {
                foreach (UIIconButton button in group.Buttons)
                    Button(batch, button);
            }
        }
        _asked.Clear();
        SideButtons.BannerToggleShown = false;
    }

    // FreshUI's places for the buttons, once per screen (their areas are lambdas, wrapped): the quick stack,
    // sort, bestiary and emote buttons in the side-button grid where the coin and ammo columns were, the
    // builder toggles after them there (SideButtons), the equipment page icons over their columns (PageIcons).
    private static void LayOut(UIGameScreen screen)
    {
        if (screen == InventoryButtons.QuickStackAll || screen == InventoryButtons.Bestiary)
            SideButtons.Move(((UIIconButtonScreen)screen).Button, 498, SideButtons.ColumnX);
        else if (screen == InventoryButtons.SortInventory || screen == InventoryButtons.Emotes)
            SideButtons.Move(((UIIconButtonScreen)screen).Button, 534, SideButtons.ColumnX + SideButtons.Pitch);
        else if (screen == InventoryIcons.PageIcons)
        {
            foreach (UIIconButton button in InventoryIcons.PageIcons.Buttons)
                PageIcons.Centre(button);
        }
        else if (screen == UIBuilderToggles.Instance)
            SideButtons.PlaceBuilderToggles(UIBuilderToggles.Instance);
    }

    private void Button(SpriteBatch batch, UIIconButton button)
    {
        if (button.Area == null || button.DrawIcon == null)
            return;
        Rectangle area = button.Area();
        bool hovered = button.ContainsPoint(Main.MouseScreen);
        if (hovered)
        {
            Main.LocalPlayer.mouseInterface = true;
            button.WhileHovered?.Invoke();
            if (button.OnPress != null && Main.mouseLeft && Main.mouseLeftRelease)
            {
                if (button.ConsumesClick)
                    Main.mouseLeftRelease = false;
                button.OnPress();
            }
            if (button.OnRightPress != null && Main.mouseRight && Main.mouseRightRelease)
            {
                Main.mouseRightRelease = false;
                button.OnRightPress();
            }
        }
        if (hovered ? _hovered.Add(button) : _hovered.Remove(button))
        {
            if (hovered && button.TickOnHover)
                SoundEngine.PlaySound(12);
        }
        FreshUI.Hooks.Sharp.WholePixels(batch, true);
        button.DrawIcon(batch, area, hovered);
        FreshUI.Hooks.Sharp.WholePixels(batch, false);
        if (hovered && button.Tooltip != null && !Main.mouseText)
            Main.instance.MouseTextNoOverride(button.Tooltip(), 0, 0);
    }
}
