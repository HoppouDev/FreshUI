using Terraria;
using Terraria.GameInput;

namespace FreshUI;

/// <summary>
/// FreshUI's keys, set once in every input profile (they're ordinary key bindings after that, changed
/// in Keybindings like any other): the inventory on E as well as Esc (Esc opens the pause menu where
/// it would have opened the inventory, see <see cref="PauseMenu" />, and still closes everything else),
/// and the grapple, which was on E, on Q.
/// </summary>
public static class KeyDefaults
{
    // In Terraria's config.json, once they're set.
    private const string Key = "FreshUIKeys";

    private const string Inventory = "E";

    private const string Grapple = "Q";

    private static bool _applied;

    /// <summary>After PlayerInput.Load (the profiles, from the game's config, which is loaded first).</summary>
    public static void Apply()
    {
        _applied = Main.Configuration.Get(Key, false);
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

    // The grapple from E to Q, then the inventory on E, each only where the key is free.
    private static void Rebind(Dictionary<string, List<string>> keys)
    {
        if (keys.TryGetValue("Grapple", out List<string>? grapple) && grapple.Contains(Inventory) && !BoundElsewhere(keys, Grapple, "Grapple"))
        {
            grapple.Remove(Inventory);
            grapple.Add(Grapple);
        }
        if (keys.TryGetValue("Inventory", out List<string>? inventory) && !inventory.Contains(Inventory) && !BoundElsewhere(keys, Inventory, "Inventory"))
        {
            inventory.Add(Inventory);
        }
    }

    private static bool BoundElsewhere(Dictionary<string, List<string>> keys, string key, string action) =>
        keys.Any(pair => pair.Key != action && pair.Value.Contains(key));

    /// <summary>Puts the setting back in Terraria's config after <c>Main.SaveSettings</c> clears it to write its own.</summary>
    public static void Store()
    {
        if (_applied)
        {
            Main.Configuration.Put(Key, true);
        }
    }
}
