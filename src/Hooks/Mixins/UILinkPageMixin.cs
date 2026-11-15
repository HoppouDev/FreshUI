using Aether.Modding;
using FreshUI.GumUi;
using Terraria.UI.Gamepad;

namespace FreshUI.Hooks.Mixins;

/// <summary>While the gamepad drives the HUD by Gum's focus (<see cref="GumHudFocus"/>), the game's own travel between its points is off.</summary>
[Mixin(typeof(UILinkPage))]
public class UILinkPageMixin
{
    [Inject("TravelUp()", At.HEAD, Cancellable = true)]
    private void Up(CallbackInfo callback) => Travel(callback);

    [Inject("TravelDown()", At.HEAD, Cancellable = true)]
    private void Down(CallbackInfo callback) => Travel(callback);

    [Inject("TravelLeft()", At.HEAD, Cancellable = true)]
    private void Left(CallbackInfo callback) => Travel(callback);

    [Inject("TravelRight()", At.HEAD, Cancellable = true)]
    private void Right(CallbackInfo callback) => Travel(callback);

    private void Travel(CallbackInfo callback)
    {
        if (GumHudFocus.TakesTravel((UILinkPage)(object)this))
            callback.Cancel();
    }
}
