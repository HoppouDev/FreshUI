using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Social;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Legacy;

/// <summary>
/// The pause menu Esc opens in game (Esc is hard-coded: it closes whatever is open, as the inventory
/// key used to, and opens the menu when nothing is; the inventory is Q):
/// Back to Game, Achievements, Mods, Settings (the settings window), Invite Friends where invites
/// work, and Save &amp; Exit. It is vanilla's settings window as far as the game is concerned
/// (<c>Main.ingameOptionsWindow</c>, which pauses the game with Autopause), showing either view.
/// There's no Settings button by the inventory (MainMixin): Settings is the pause menu's.
/// </summary>
public static class PauseMenu
{
    public enum View
    {
        Pause,
        Settings
    }

    /// <summary>What the window shows.</summary>
    public static View Current;

    // Whether the settings were opened from the pause menu (their Back returns to it).
    private static bool _settingsFromPause;

    /// <summary>Whether the settings were opened from the pause menu (the inventory key and B go back to it).</summary>
    public static bool SettingsFromPause => Current == View.Settings && _settingsFromPause;

    // Whether the inventory was open when the window opened: it's left as it was when the window closes
    // (vanilla always reopens it, as its settings window was opened from the inventory).
    private static bool _inventoryOpen;

    // The view to come back to when the fancy UI opened from the window (achievements, mods, key bindings) closes.
    private static View? _returnTo;

    // Whether Esc is being handled (rather than the inventory key).
    private static bool _escape;

    // Whether Start was held at the last update.
    private static bool _startWasDown;

    /// <summary>
    /// Esc, every update after the keyboard is read (before chat, which closes on Esc itself): what the
    /// inventory key does to close what's open (the inventory, a chest, NPC chat, the map, the window,
    /// a fancy UI), except that with nothing open it opens the pause menu.
    /// </summary>
    public static void HandleEscape()
    {
        // The gamepad's Start does the same (it's the pause button, as in most games).
        bool start = GamePad.GetState(PlayerIndex.One).IsButtonDown(Buttons.Start);
        bool startPressed = start && !_startWasDown;
        _startWasDown = start;
        bool escape = Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape);
        if (Main.gameMenu || !escape && !startPressed)
        {
            return;
        }
        if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.CurrentlyRebinding)
        {
            return;
        }
        _escape = true;
        try
        {
            Main.LocalPlayer.ToggleInv();
        }
        finally
        {
            _escape = false;
        }
    }

    /// <summary>Opens the pause menu.</summary>
    public static void Open()
    {
        IngameOptions.Open();
        Current = View.Pause;
    }

    /// <summary>What closing what's open does when nothing is: Esc opens the pause menu, the inventory key the inventory.</summary>
    public static void OpenInventoryOrPause(bool quiet)
    {
        if (_escape)
        {
            Open();
        }
        else
        {
            Player.OpenInventory(quiet);
        }
    }

    /// <summary>What the inventory key does while the window is open: back from settings opened from the pause menu, else close.</summary>
    public static void CloseOrBack()
    {
        if (Current == View.Settings && _settingsFromPause)
        {
            Back();
        }
        else
        {
            IngameOptions.Close();
        }
    }

    /// <summary>The settings' Back: to the pause menu if they were opened from it, else closed.</summary>
    public static void Back()
    {
        if (Current == View.Settings && _settingsFromPause)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Current = View.Pause;
        }
        else
        {
            IngameOptions.Close();
        }
    }

    public static void ShowSettings()
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        Current = View.Settings;
        _settingsFromPause = true;
    }

    /// <summary>Comes back to <paramref name="view" /> when the fancy UI about to open closes.</summary>
    public static void ReturnAfter(View view) => _returnTo = view;

    // --- hooks ---------------------------------------------------------------------------------

    /// <summary>IngameOptions.Open, before it closes the inventory.</summary>
    public static void Opening()
    {
        if (!Main.ingameOptionsWindow)
        {
            _inventoryOpen = Main.playerInventory;
        }
    }

    /// <summary>IngameOptions.Open, done: the pause menu, afresh.</summary>
    public static void Opened()
    {
        Current = View.Pause;
        _settingsFromPause = false;
        OpenCount++;
    }

    /// <summary>How many times the window has opened: the Gum pause menu rebuilds its buttons for each.</summary>
    public static int OpenCount { get; private set; }

    /// <summary>Whether the pause menu (<see cref="FreshUI.Screens.PauseScreen"/>) shows: the game's settings window is open as the pause menu, with no fancy UI over it.</summary>
    public static bool ShowsPause => !Main.gameMenu && Main.ingameOptionsWindow && Current == View.Pause && !Main.InGameUI.IsVisible;

    /// <summary>Whether the in-game settings (<see cref="FreshUI.Screens.SettingsScreen"/>) show: the game's settings window is open as the settings.</summary>
    public static bool ShowsSettings => !Main.gameMenu && Main.ingameOptionsWindow && Current == View.Settings && !Main.InGameUI.IsVisible;

    /// <summary>IngameOptions.Close: the inventory as it was before the window opened.</summary>
    public static void Closed()
    {
        if (!Main.ingameOptionsWindow)
        {
            Main.playerInventory = _inventoryOpen;
        }
    }

    /// <summary>IngameFancyUI.Close: back to the window if the fancy UI was opened from it.</summary>
    public static void FancyClosed()
    {
        if (_returnTo is not View view || Main.gameMenu)
        {
            _returnTo = null;
            return;
        }
        _returnTo = null;
        bool inventory = _inventoryOpen;
        bool fromPause = _settingsFromPause;
        IngameOptions.Open();
        _inventoryOpen = inventory;
        _settingsFromPause = fromPause;
        Current = view;
    }

    /// <summary>Where the open window is, title included, in the game's UI units (for IngameOptions' checks of the mouse being over it).</summary>
    public static Rectangle Area;

    /// <summary>Sets <see cref="Area"/> to a Gum window's (UI pixels, two UI units each).</summary>
    internal static void SetArea(Gum.Wireframe.GraphicalUiElement window)
    {
        float lift = FreshUI.Components.FreshWindow.TitleLift;
        Area = new Rectangle((int)(window.AbsoluteLeft * 2), (int)((window.AbsoluteTop - lift) * 2), (int)(window.AbsoluteWidth * 2), (int)((window.AbsoluteHeight + lift) * 2));
    }

    // --- actions -------------------------------------------------------------------------------

    internal static void Achievements()
    {
        ReturnAfter(View.Pause);
        IngameOptions.Close();
        IngameFancyUI.OpenAchievements();
    }

    // Aether's Mods window (internal to the game), as a fancy UI.
    private static readonly Type? ModListType = typeof(Main).Assembly.GetType("Aether.UI.Menu.UIModList");

    internal static bool HasMods => ModListType != null;

    internal static void Mods()
    {
        if (ModListType == null || Activator.CreateInstance(ModListType, nonPublic: true) is not UIState mods)
        {
            return;
        }
        ReturnAfter(View.Pause);
        IngameOptions.Close(quiet: true);
        SoundEngine.PlaySound(SoundID.MenuOpen);
        IngameFancyUI.OpenUIState(mods);
    }

    internal static bool CanInvite => SocialAPI.Network != null && SocialAPI.Network.CanInvite();

    internal static void Invite()
    {
        IngameOptions.Close();
        SocialAPI.Network.OpenInviteInterface();
    }

    internal static void SaveAndExit()
    {
        IngameOptions.Close();
        Main.menuMode = 10;
        Main.gameMenu = true;
        WorldGen.SaveAndQuit();
    }
}
