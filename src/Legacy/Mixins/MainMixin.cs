using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The UI scale is FreshUI's GUI scale, whatever the game asks for (the title menu, the setting), and
/// the GUI scale and key settings survive the game saving its settings; Esc is hard-coded to close
/// what's open and open the pause menu; there's no Settings button (the pause menu has Settings); and
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
    private static void KeepGuiScale(CallbackInfoReturnable<bool> callback)
    {
        GuiScale.Store();
        KeyDefaults.Store();
    }

    // Esc, hard-coded: closes what's open, else opens the pause menu.
    [Inject("DoUpdate_HandleInput(Microsoft.Xna.Framework.GameTime)", At.TAIL)]
    private static void Escape(GameTime gameTime, CallbackInfo callback) => PauseMenu.HandleEscape();

    // No Settings button at the bottom right (inventory, dead, over the settings window): Esc's pause
    // menu has Settings.
    [Redirect("DrawInterface_29_SettingsButton()", At.INVOKE, Target = "Terraria.Main::DrawSettingButton(System.Boolean&,System.Single&,System.Int32,System.Int32,System.String,System.String,System.Action)")]
    private static void NoSettingsButton(ref bool mouseOver, ref float scale, int posX, int posY, string text, string textSizeMatcher, Action clickAction)
    {
    }

    /// <summary>How much lower FreshUI puts an open chest or shop (and what's beside it): one inventory row.</summary>
    public const int ChestDrop = (int)PixelSlots.Pitch;

    // Main.invBottom, the top of an open chest's or shop's slots (vanilla: 258).
    [ModifyConstant("Initialize_AlmostEverything()", "int:258")]
    private static int InventoryBottom(int bottom) => bottom + ChestDrop;
}
