using Aether.Modding;
using FreshUI.GumUi;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace FreshUI.Hooks.Mixins;

/// <summary>A user interface whose state a Gum screen replaces (<see cref="GumRoutes.State{T}"/>) neither updates nor draws.</summary>
[Mixin(typeof(UserInterface))]
public class UserInterfaceMixin
{
    [Inject("Update(Microsoft.Xna.Framework.GameTime,System.Boolean)", At.HEAD, Cancellable = true)]
    private void Update(GameTime time, bool allowMouse, CallbackInfo callback)
    {
        if (GumRoutes.IsState(((UserInterface)(object)this).CurrentState))
            callback.Cancel();
    }

    // (Its draw can't be cancelled at its head: the state's own draw is skipped instead.)
    [Redirect("Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch,Microsoft.Xna.Framework.GameTime)", At.INVOKE,
        Target = "Terraria.UI.UIElement::Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch)")]
    private static void DrawState(UIElement state, SpriteBatch batch)
    {
        if (!GumRoutes.IsState(state as UIState))
            state.Draw(batch);
    }
}
