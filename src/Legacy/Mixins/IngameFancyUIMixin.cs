using Aether.Modding;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>Achievements, mods and key bindings opened from the pause menu or settings go back to them when they close.</summary>
[Mixin(typeof(IngameFancyUI))]
public static class IngameFancyUIMixin
{
    [Inject("Close(System.Boolean)", At.TAIL)]
    private static void Closed(bool quiet, CallbackInfo callback) => PauseMenu.FancyClosed();
}
