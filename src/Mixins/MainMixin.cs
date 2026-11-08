using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;

namespace FreshUI.Mixins;

/// <summary>
/// Whole-number UI scales, and the compact top-right HUD: the minimap, life and mana and (inventory
/// closed) the info readouts under the minimap are drawn at <see cref="Hud.CompactScale" /> of the UI
/// scale. They lay themselves out from the screen's right edge, so they stay in the top right corner,
/// and the mouse is in the same smaller coordinates meanwhile.
/// </summary>
[Mixin(typeof(Main))]
public static class MainMixin
{
    [Shadow]
    private static float _uiScaleUsed;

    [Shadow]
    private static Matrix _uiScaleMatrix;

    // The UI scale before a compact section, restored after it.
    private static float _freshUiScale;

    private static bool _freshCompactInfo;

    [Redirect("set_UIScale(System.Single)", At.FIELD, Target = "Terraria.Main::_uiScaleUsed")]
    private static void SetScaleUsed(float value) => _uiScaleUsed = PixelScale.Whole(value, Main.instance.UIScaleMax);

    [Redirect("set_UIScale(System.Single)", At.FIELD, Target = "Terraria.Main::_uiScaleMatrix")]
    private static void SetScaleMatrix(Matrix matrix) => _uiScaleMatrix = Matrix.CreateScale(_uiScaleUsed, _uiScaleUsed, 1f);

    // Ends the UI sprite batch and begins one at the compact scale (the scale as is, no rounding).
    private static void FreshCompactBegin()
    {
        Main.spriteBatch.End();
        _freshUiScale = _uiScaleUsed;
        FreshSetScale(_uiScaleUsed * Hud.CompactScale);
        PlayerInput.SetZoom_UI();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, Sharp.Sampler(null), null, null, null, _uiScaleMatrix);
    }

    // Ends the compact sprite batch and begins the UI one again, at the UI scale.
    private static void FreshCompactEnd()
    {
        Main.spriteBatch.End();
        FreshSetScale(_freshUiScale);
        PlayerInput.SetZoom_UI();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, Sharp.Sampler(null), null, null, null, _uiScaleMatrix);
    }

    private static void FreshSetScale(float scale)
    {
        _uiScaleUsed = scale;
        _uiScaleMatrix = Matrix.CreateScale(scale, scale, 1f);
    }

    [Inject("GUIBarsDrawInner()", At.INVOKE, Target = "Aether.UI.Game.UIResourceBars::Draw()")]
    private static void BeforeResourceBars(CallbackInfo callback) => FreshCompactBegin();

    // The breath bubbles and buffs after them keep the UI scale.
    [Inject("GUIBarsDrawInner()", At.INVOKE, Target = "Microsoft.Xna.Framework.Graphics.SpriteBatch::End()", Ordinal = 0)]
    private static void AfterResourceBars(CallbackInfo callback) => FreshCompactEnd();

    // The minimap's height, which the equipment page starts below.
    [ModifyConstant("DrawInterface_16_MapOrMinimap()", "int:256")]
    private static int MinimapHeight(int height) => (int)(height * Hud.CompactScale);

    [Inject("DrawInterface_16_MapOrMinimap()", At.INVOKE, Target = "Terraria.Main::DrawMap(Microsoft.Xna.Framework.GameTime)")]
    private static void BeforeMinimap(CallbackInfo callback) => FreshCompactBegin();

    [Inject("DrawInterface_16_MapOrMinimap()", At.INVOKE, Target = "Terraria.Main::DrawMap(Microsoft.Xna.Framework.GameTime)", Shift = AtShift.AFTER)]
    private static void AfterMinimap(CallbackInfo callback) => FreshCompactEnd();

    // Under the minimap (inventory closed), laid out against the compact minimap as vanilla lays
    // them out against its full-size one.
    [Inject("DrawInterface_28_InfoAccs()", At.INVOKE, Target = "Terraria.Main::DrawInfoAccs()")]
    private static void BeforeInfoAccs(CallbackInfo callback)
    {
        _freshCompactInfo = !Main.playerInventory;
        if (_freshCompactInfo)
            FreshCompactBegin();
    }

    [Inject("DrawInterface_28_InfoAccs()", At.INVOKE, Target = "Terraria.Main::DrawInfoAccs()", Shift = AtShift.AFTER)]
    private static void AfterInfoAccs(CallbackInfo callback)
    {
        if (_freshCompactInfo)
            FreshCompactEnd();
        _freshCompactInfo = false;
    }
}
