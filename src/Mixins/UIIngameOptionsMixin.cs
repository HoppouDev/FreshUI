using Aether.Modding;
using Aether.UI.Game;
using Aether.UI.Menu;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>The settings window's UI scale slider, replaced by a button that cycles the GUI scale.</summary>
[Mixin(typeof(UIIngameOptions))]
public class UIIngameOptionsMixin
{
    [Shadow]
    private void Toggle(int slot, Func<string> text, Action change, float scale) => throw new NotSupportedException();

    // The fifth slider on the General page: music, sound, ambient, zoom, then the UI scale.
    [Redirect("BuildGeneral()", At.INVOKE, Target = "Aether.UI.Game.UIIngameOptions::Slider(System.Int32,System.Func`1<System.String>,System.Func`1<System.Single>,System.Action`1<System.Single>,System.Int32,System.Single,System.Single,Terraria.Utils/ColorLerpMethod,System.Int32)", Ordinal = 4)]
    private UIMenuSlider? GuiScaleButton(UIIngameOptions options, int slot, Func<string> label, Func<float> getValue, Action<float> setValue, int gamepadFeature, float labelScale, float barShift, Utils.ColorLerpMethod colorMethod, int tag)
    {
        Toggle(slot, GuiScale.LabelText, GuiScale.Cycle, labelScale);
        return null;
    }
}
