using Aether.Modding;
using Terraria.GameInput;

namespace FreshUI.Mixins;

/// <summary>FreshUI's keys (<see cref="KeyDefaults" />), set once the input profiles are loaded.</summary>
[Mixin(typeof(PlayerInput))]
public static class PlayerInputMixin
{
    [Inject("Load()", At.TAIL)]
    private static void Loaded(CallbackInfo callback) => KeyDefaults.Apply();
}
