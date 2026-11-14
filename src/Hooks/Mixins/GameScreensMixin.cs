using Aether.Modding;
using Aether.UI.Game;
using FreshUI.GumUi;
using FreshUI.Screens;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Hooks.Mixins;

/// <summary>Aether's in-game screens that Gum screens replace aren't drawn or updated (<see cref="GumRoutes.GameScreen{T}"/>); the live inventory's slots are laid out on Gum's grid before their hit testing.</summary>
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

    [Inject(Draw, At.INVOKE, Target = "Terraria.UI.UserInterface::Update(Microsoft.Xna.Framework.GameTime)")]
    private static void Layout(UIGameScreen screen, SpriteBatch batch, CallbackInfo callback) => HudScreen.LayoutSlots(screen);
}
