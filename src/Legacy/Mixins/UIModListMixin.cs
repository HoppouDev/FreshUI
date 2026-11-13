using Aether.Modding;
using Terraria;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Aether's Mods window, opened in game from the pause menu: its Back (and Esc) closes it there, back to the pause menu.</summary>
[Mixin("Aether.UI.Menu.UIModList")]
public static class UIModListMixin
{
    [Inject("HandleBackButtonUsage()", At.HEAD, Cancellable = true)]
    private static void Back(CallbackInfo callback)
    {
        if (!Main.gameMenu)
        {
            IngameFancyUI.Close();
            callback.Cancel();
        }
    }
}
