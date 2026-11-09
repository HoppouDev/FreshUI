using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>The UI scale is FreshUI's GUI scale, whatever the game asks for (the title menu, the setting).</summary>
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
}
