using Terraria;
using Terraria.GameInput;

namespace FreshUI.Legacy;

/// <summary>
/// FreshUI's keys, set once in every input profile's keyboard modes (they're ordinary key bindings
/// after that, changed in Keybindings like any other): the inventory on Q and the grapple on E, and Esc
/// off the inventory (it's hard-coded to close what's open and open the pause menu, see
/// <see cref="PauseMenu" />). Earlier versions put the inventory on E and the grapple on Q; those
/// profiles are moved too.
/// </summary>
public static class KeyDefaults
{
    // In Terraria's config.json: the version of FreshUI's keys set in the profiles.
    private const string Key = "FreshUIKeyVersion";

    private const int Version = 2;

    private const string Escape = "Escape";

    private const string InventoryKey = "Q";

    private const string GrappleKey = "E";

    private static bool _applied;

    /// <summary>After PlayerInput.Load (the profiles, from the game's config, which is loaded first).</summary>
    public static void Apply()
    {
        _applied = Main.Configuration.Get(Key, 0) >= Version;
        if (_applied)
        {
            return;
        }
        foreach (PlayerInputProfile profile in PlayerInput.Profiles.Values)
        {
            foreach (InputMode mode in new[] { InputMode.Keyboard, InputMode.KeyboardUI })
            {
                if (profile.InputModes.TryGetValue(mode, out KeyConfiguration? configuration))
                {
                    Rebind(configuration.KeyStatus);
                }
            }
        }
        PlayerInput.Save();
        _applied = true;
        Store();
        Main.Configuration.Save();
    }

    // Esc and the earlier keys off, then each action on its key where nothing else has it.
    private static void Rebind(Dictionary<string, List<string>> keys)
    {
        keys.TryGetValue("Inventory", out List<string>? inventory);
        keys.TryGetValue("Grapple", out List<string>? grapple);
        inventory?.Remove(Escape);
        inventory?.Remove(GrappleKey);
        grapple?.Remove(InventoryKey);
        Bind(keys, inventory, "Inventory", InventoryKey);
        Bind(keys, grapple, "Grapple", GrappleKey);
    }

    private static void Bind(Dictionary<string, List<string>> keys, List<string>? action, string name, string key)
    {
        if (action != null && !action.Contains(key) && !keys.Any(pair => pair.Key != name && pair.Value.Contains(key)))
        {
            action.Add(key);
        }
    }

    /// <summary>Puts the setting back in Terraria's config after <c>Main.SaveSettings</c> clears it to write its own.</summary>
    public static void Store()
    {
        if (_applied)
        {
            Main.Configuration.Put(Key, Version);
        }
    }
}
