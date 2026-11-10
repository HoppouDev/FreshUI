using Aether.Modding;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;

namespace FreshUI.Mixins;

/// <summary>Scrollbars glide each frame after a wheel turn (<see cref="MomentumScroll" />).</summary>
[Mixin(typeof(UIScrollbar))]
public class UIScrollbarMixin
{
#pragma warning disable CS0649
    [Shadow]
    private bool _isDragging;
#pragma warning restore CS0649

    [Inject("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD)]
    private void Glide(SpriteBatch spriteBatch, CallbackInfo callback) => MomentumScroll.Step((UIScrollbar)(object)this, _isDragging);
}
