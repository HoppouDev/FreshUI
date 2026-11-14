using Aether.Modding;
using Aether.UI.Game;
using FreshUI.GumUi;
using FreshUI.Screens;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Hooks.Mixins;

/// <summary>Aether's in-game screens that Gum screens replace aren't drawn or updated (<see cref="GumRoutes.GameScreen{T}"/>).</summary>
[Mixin(typeof(GameScreens))]
public static class GameScreensMixin
{
    private const string Draw = "Draw(Aether.UI.Game.UIGameScreen,Microsoft.Xna.Framework.Graphics.SpriteBatch)";

    [Inject(Draw, At.HEAD, Cancellable = true)]
    private static void Replaced(UIGameScreen screen, SpriteBatch batch, CallbackInfo callback)
    {
        if (GumRoutes.AskGameScreen(screen) || HudScreen.IsReady && screen == InventoryButtons.HotbarLock)
            callback.Cancel();
    }

}
