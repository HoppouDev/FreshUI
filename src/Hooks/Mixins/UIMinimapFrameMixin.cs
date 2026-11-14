using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Minimap;

namespace FreshUI.Hooks.Mixins;

/// <summary>The minimap's frame and its buttons' hover art are drawn by Gum (<see cref="HudScreen.TakeMinimapFrame"/>), not here.</summary>
[Mixin(typeof(UIMinimapFrame))]
public static class UIMinimapFrameMixin
{
    [Inject("DrawForeground(Terraria.GameContent.UI.Minimap.MinimapFrame,Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD, Cancellable = true)]
    private static void DrawForeground(MinimapFrame frame, SpriteBatch batch, CallbackInfoReturnable<bool> callback)
    {
        if (HudScreen.TakeMinimapFrame(frame))
            callback.SetReturnValue(true);
    }
}
