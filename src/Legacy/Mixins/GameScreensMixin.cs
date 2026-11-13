using Aether.Modding;
using Aether.UI.Game;
using FreshUI.Screens;
using Microsoft.Xna.Framework.Graphics;

namespace FreshUI.Legacy.Mixins;

/// <summary>Apply the original Gum inventory grid to the live slots before native hit testing.</summary>
[Mixin(typeof(GameScreens))]
public static class GameScreensMixin
{
    private const string Draw = "Draw(Aether.UI.Game.UIGameScreen,Microsoft.Xna.Framework.Graphics.SpriteBatch)";

    [Inject(Draw, At.HEAD, Cancellable = true)]
    private static void GumLock(UIGameScreen screen, SpriteBatch batch, CallbackInfo callback)
    {
        if (HudScreen.IsReady && screen == InventoryButtons.HotbarLock)
            callback.Cancel();
    }

    [Inject(Draw, At.INVOKE, Target = "Terraria.UI.UserInterface::Update(Microsoft.Xna.Framework.GameTime)")]
    private static void Layout(UIGameScreen screen, SpriteBatch batch, CallbackInfo callback) => HudScreen.LayoutSlots(screen);
}
