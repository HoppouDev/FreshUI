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
        GameScreens.Close(PauseScreen.Instance);
        GameScreens.Close(GameSettingsScreen.Instance);
    }

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

    /// <summary>The screen to draw for the window.</summary>
    public static UIGameScreen Screen => Current == View.Pause ? PauseScreen.Instance : GameSettingsScreen.Instance;

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

/// <summary>The pause menu's screen: the game dimmed, and the menu's buttons in a panel under a title tab, as the Mods window's.</summary>
public sealed class PauseScreen : UIGameScreen
{
    private static PauseScreen? _instance;

    public static PauseScreen Instance => _instance ??= new PauseScreen();

    private const float Width_ = 500f;

    private const float Padding = 16f;

    private const float Gap = 10f;

    private UIPanel? _panel;

    private UITextPanel<string>? _title;

    private readonly List<List<UITextPanel<string>>> _rows = new();

    public override void OnActivate()
    {
        RemoveAllChildren();
        _rows.Clear();
        _panel = new UIPanel
        {
            BackgroundColor = SettingsWindow.PanelColor
        };
        _panel.SetPadding(Padding);
        Append(_panel);
        Row(("Back to Game", () => IngameOptions.Close()));
        if (PauseMenu.HasMods)
        {
            Row((Lang.menu[131].Value, PauseMenu.Achievements), (Language.GetTextValue("AetherMods.Title"), PauseMenu.Mods));
        }
        else
        {
            Row((Lang.menu[131].Value, PauseMenu.Achievements));
        }
        if (PauseMenu.CanInvite)
        {
            Row((Lang.menu[14].Value, PauseMenu.ShowSettings), (Lang.menu[147].Value, PauseMenu.Invite));
        }
        else
        {
            Row((Lang.menu[14].Value, PauseMenu.ShowSettings));
        }
        Row((Lang.inter[35].Value, PauseMenu.SaveAndExit));
        _title = new UITextPanel<string>("Game Menu", 0.8f, large: true)
        {
            HAlign = 0.5f,
            BackgroundColor = SettingsWindow.HoverColor,
            IgnoresMouseInteraction = true
        };
        _title.SetPadding(12f);
        Append(_title);
    }

    private void Row(params (string Text, Action OnClick)[] buttons)
    {
        List<UITextPanel<string>> row = new();
        foreach ((string text, Action onClick) in buttons)
        {
            UITextPanel<string> button = SettingsWindow.ActionButton(text, onClick);
            row.Add(button);
            _panel!.Append(button);
        }
        _rows.Add(row);
    }

    private static float Even(float value) => MathF.Floor(value / 2f) * 2f;

    public override void Recalculate()
    {
        // The screen's own size first: the panel is placed in it (after a GUI scale change, the old one is stale).
        base.Recalculate();
        if (_panel != null && _title != null)
        {
            CalculatedStyle screen = GetDimensions();
            float inner = Width_ - Padding * 2f;
            float height = Padding * 2f + _rows.Count * SettingsWindow.ActionHeight + (_rows.Count - 1) * Gap;
            float top = Even((screen.Height - height) / 2f);
            _panel.Left.Set(Even((screen.Width - Width_) / 2f), 0f);
            _panel.Top.Set(top, 0f);
            _panel.Width.Set(Width_, 0f);
            _panel.Height.Set(height, 0f);
            for (int i = 0; i < _rows.Count; i++)
            {
                List<UITextPanel<string>> row = _rows[i];
                float width = Even((inner - Gap * (row.Count - 1)) / row.Count);
                for (int j = 0; j < row.Count; j++)
                {
                    // The last one ends at the right edge, whatever rounding left over.
                    row[j].Left.Set(j == row.Count - 1 ? inner - width : j * (width + Gap), 0f);
                    row[j].Top.Set(i * (SettingsWindow.ActionHeight + Gap), 0f);
                    row[j].Width.Set(width, 0f);
                    row[j].Height.Set(SettingsWindow.ActionHeight, 0f);
                }
            }
            _title.Top.Set(top - 40f, 0f);
            RecalculateChildren();
        }
    }

    public override void UpdateState()
    {
        if (_panel == null || _title == null)
        {
            return;
        }
        CalculatedStyle panel = _panel.GetDimensions();
        CalculatedStyle title = _title.GetDimensions();
        SettingsWindow.Area = new Rectangle((int)panel.X, (int)title.Y, (int)panel.Width, (int)(panel.Y + panel.Height - title.Y));
        if (SettingsWindow.Area.Contains(Main.MouseScreen.ToPoint()))
        {
            Main.LocalPlayer.mouseInterface = true;
        }
        // The gamepad: each button, linked to its neighbours by where it is (GamepadMenu).
        foreach (List<UITextPanel<string>> row in _rows)
        {
            foreach (UITextPanel<string> button in row)
            {
                GamepadMenu.Add(button);
            }
        }
        GamepadMenu.Publish(this, _ => GamepadMenu.Hint(Lang.misc[53].Value, "MouseLeft") + GamepadMenu.Hint(Lang.misc[56].Value, "Inventory"));
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) => Dim(spriteBatch, this);

    /// <summary>Darkens the game behind the window.</summary>
    public static void Dim(SpriteBatch spriteBatch, UIElement screen)
    {
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, screen.GetDimensions().ToRectangle(), Color.Black * 0.5f);
    }
}
