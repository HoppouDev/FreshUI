using Aether.Modding;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>The defense counter's shield at whole pixels (it's drawn at the inventory's scale).</summary>
[Mixin(typeof(Main))]
public static class DefenseCounterMixin
{
    [Inject("DrawDefenseCounter(System.Int32,System.Int32)", At.HEAD)]
    private static void Before(CallbackInfo callback) => Sharp.WholePixels(Main.spriteBatch, true);

    [Inject("DrawDefenseCounter(System.Int32,System.Int32)", At.RETURN)]
    private static void After(CallbackInfo callback) => Sharp.WholePixels(Main.spriteBatch, false);
}
