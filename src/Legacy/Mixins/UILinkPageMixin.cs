using Aether.Modding;
using Microsoft.Xna.Framework;
using Terraria.UI.Gamepad;

namespace FreshUI.Legacy.Mixins;

/// <summary>The D-pad in the inventory goes to the nearest point drawn that way (<see cref="InventoryNavigation"/>).</summary>
[Mixin(typeof(UILinkPage))]
public class UILinkPageMixin
{
    [Inject("TravelUp()", At.HEAD, Cancellable = true)]
    private void Up(CallbackInfo callback) => Travel(-Vector2.UnitY, callback);

    [Inject("TravelDown()", At.HEAD, Cancellable = true)]
    private void Down(CallbackInfo callback) => Travel(Vector2.UnitY, callback);

    [Inject("TravelLeft()", At.HEAD, Cancellable = true)]
    private void Left(CallbackInfo callback) => Travel(-Vector2.UnitX, callback);

    [Inject("TravelRight()", At.HEAD, Cancellable = true)]
    private void Right(CallbackInfo callback) => Travel(Vector2.UnitX, callback);

    private void Travel(Vector2 direction, CallbackInfo callback)
    {
        if (InventoryNavigation.Travel((UILinkPage)(object)this, direction))
            callback.Cancel();
    }
}
