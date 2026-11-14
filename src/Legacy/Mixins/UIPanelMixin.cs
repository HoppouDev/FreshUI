using Aether.Modding;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>The game's panels on whole UI pixels, as Gum's (<see cref="PanelArt.Snap"/>).</summary>
[Mixin(typeof(UIPanel))]
public static class UIPanelMixin
{
    [Redirect("DrawPanel(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.Graphics.Texture2D,Microsoft.Xna.Framework.Color)", At.INVOKE,
        Target = "Terraria.UI.UIElement::GetDimensions()")]
    private static CalculatedStyle Dimensions(UIElement panel) => PanelArt.Snap(panel.GetDimensions());
}
