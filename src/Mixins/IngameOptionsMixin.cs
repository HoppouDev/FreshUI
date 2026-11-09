using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Mixins;

/// <summary>Vanilla's checks for the mouse being over the settings window (<c>IngameOptions.MouseOver</c>) see FreshUI's window.</summary>
[Mixin(typeof(IngameOptions))]
public static class IngameOptionsMixin
{
    [Shadow]
    private static Rectangle _GUIHover;

    [Inject("Draw(Terraria.Main,Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD)]
    private static void Hover(CallbackInfo callback) => _GUIHover = SettingsWindow.Area;
}
