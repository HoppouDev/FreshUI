using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>
/// The UI scale is FreshUI's GUI scale, whatever the game asks for (the title menu, the setting), and
/// the GUI scale setting survives the game saving its settings; and
/// an open chest or shop is a row lower, under the coin and ammo row FreshUI adds below the inventory.
/// </summary>
[Mixin(typeof(Main))]
public static class MainMixin
{
    [Shadow]
    private static float _uiScaleUsed;

    [Shadow]
    private static Matrix _uiScaleMatrix;

    [Redirect("set_UIScale(System.Single)", At.FIELD, Target = "Terraria.Main::_uiScaleUsed")]
    private static void SetScaleUsed(float value) => _uiScaleUsed = GuiScale.UIScale;

    [Redirect("set_UIScale(System.Single)", At.FIELD, Target = "Terraria.Main::_uiScaleMatrix")]
    private static void SetScaleMatrix(Matrix matrix) => _uiScaleMatrix = Matrix.CreateScale(_uiScaleUsed, _uiScaleUsed, 1f);

    // Main.SaveSettings clears the config and writes only the game's settings: read the GUI scale before
    // (it's read lazily) and put it back after.
    [Inject("SaveSettings()", At.HEAD)]
    private static void LoadGuiScale(CallbackInfoReturnable<bool> callback) => _ = GuiScale.Setting;

    [Inject("SaveSettings()", At.INVOKE, Target = "Terraria.IO.Preferences::Clear()", Shift = AtShift.AFTER)]
    private static void KeepGuiScale(CallbackInfoReturnable<bool> callback) => GuiScale.Store();

    /// <summary>How much lower FreshUI puts an open chest or shop (and what's beside it): one inventory row.</summary>
    public const int ChestDrop = (int)PixelSlots.Pitch;

    // Main.invBottom, the top of an open chest's or shop's slots (vanilla: 258).
    [ModifyConstant("Initialize_AlmostEverything()", "int:258")]
    private static int InventoryBottom(int bottom) => bottom + ChestDrop;
}
