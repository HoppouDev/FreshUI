using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// Vanilla's settings window as FreshUI's pause menu and settings: its checks for the mouse being over
/// the window (<c>IngameOptions.MouseOver</c>) see FreshUI's, and closing it leaves the inventory as it
/// was when it opened (vanilla always reopens it).
/// </summary>
[Mixin(typeof(IngameOptions))]
public static class IngameOptionsMixin
{
    [Shadow]
    private static Rectangle _GUIHover;

    [Inject("Draw(Terraria.Main,Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD)]
    private static void Hover(CallbackInfo callback) => _GUIHover = PauseMenu.Area;

    [Inject("Open()", At.HEAD)]
    private static void Opening(CallbackInfo callback) => PauseMenu.Opening();

    [Inject("Close(System.Boolean)", At.TAIL)]
    private static void Closed(bool quiet, CallbackInfo callback) => PauseMenu.Closed();
}
