using Aether.Modding;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// Closing what's open (Player.ToggleInv: the inventory key, and Esc, see <see cref="PauseMenu" />) as
/// FreshUI's pause menu has it: where it would open the inventory, Esc opens the pause menu instead;
/// and in the window, it goes back from the settings to the pause menu they were opened from before it
/// closes the window.
/// </summary>
[Mixin(typeof(Player))]
public static class PlayerMixin
{
    [Redirect("ToggleInv()", At.INVOKE, Target = "Terraria.Player::OpenInventory(System.Boolean)")]
    private static void OpenInventory(bool quiet) => PauseMenu.OpenInventoryOrPause(quiet);

    [Redirect("ToggleInv()", At.INVOKE, Target = "Terraria.IngameOptions::Close()")]
    private static void CloseOptions() => PauseMenu.CloseOrBack();

    // While dead, the inventory key opens and closes the settings window: the pause menu, here.
    [Redirect("TryOpeningInGameOptionsBasedOnInput()", At.INVOKE, Target = "Terraria.IngameOptions::Open()")]
    private static void OpenOptionsWhileDead() => PauseMenu.Open();

    [Redirect("TryOpeningInGameOptionsBasedOnInput()", At.INVOKE, Target = "Terraria.IngameOptions::Close()")]
    private static void CloseOptionsWhileDead() => PauseMenu.CloseOrBack();
}
