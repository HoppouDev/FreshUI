using FreshUI.GumUi;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Terraria;

namespace FreshUI.Components;

/// <summary>
/// A Gum element that draws game content in its place, in Gum's draw order: <see cref="Draw"/> gets the
/// game's sprite batch, begun in the game's UI units (two a UI pixel) with FreshUI's sharp sampler, and
/// the element's area in those units. For what only the game can draw: chat text with its tags (item
/// icons, colours), NPC portraits, coins, item and buff art at the game's own scales.
/// </summary>
class FreshGameView : InteractiveGue
{
    /// <summary>Draws the content within (or around) the given area, in UI units.</summary>
    public Action<SpriteBatch, Rectangle>? Draw { get; set; }

    public FreshGameView() : this(new GameRenderable())
    {
    }

    private FreshGameView(GameRenderable renderable) : base(renderable)
    {
        renderable.View = this;
        HasEvents = false;
    }

    private sealed class GameRenderable : InvisibleRenderable
    {
        public FreshGameView? View;

        private static readonly RasterizerState Clipped = new() { CullMode = CullMode.CullCounterClockwiseFace, ScissorTestEnable = true };

        // Its own batch key: Gum ends the batch it was drawing in before this draws, so what Gum drew
        // earlier stays under the game's content and what it draws later goes over it.
        public override string BatchKey => "FreshUI.GameView";

        public override void Render(ISystemManagers managers)
        {
            if (View?.Draw is not { } draw || !View.AbsoluteVisible)
                return;
            float units = UiPixels.Scale / 2f;
            SpriteBatch batch = Main.spriteBatch;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            // Clipped as Gum clips its own drawing: to every ancestor that clips its children (a scroll area's view).
            Rectangle clip = device.Viewport.Bounds;
            for (var parent = View.Parent as GraphicalUiElement; parent != null; parent = parent.Parent as GraphicalUiElement)
            {
                if (parent.ClipsChildren)
                    clip = Rectangle.Intersect(clip, new Rectangle((int)MathF.Round(parent.AbsoluteLeft * UiPixels.Scale), (int)MathF.Round(parent.AbsoluteTop * UiPixels.Scale),
                        (int)MathF.Round(parent.GetAbsoluteWidth() * UiPixels.Scale), (int)MathF.Round(parent.GetAbsoluteHeight() * UiPixels.Scale)));
            }
            if (clip.Width <= 0 || clip.Height <= 0)
                return;
            Rectangle scissor = device.ScissorRectangle;
            device.ScissorRectangle = clip;
            // The area in whole game units, the rest of its place in the transform: drawn where Gum puts it (on whole screen
            // pixels, as the rest), not a game unit (half a UI pixel) apart from it.
            float left = View.AbsoluteLeft * 2, top = View.AbsoluteTop * 2;
            int x = (int)MathF.Round(left), y = (int)MathF.Round(top);
            batch.Begin(SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, FreshUI.Hooks.Sharp.Snapped, DepthStencilState.None,
                Clipped, null, Matrix.CreateTranslation(left - x, top - y, 0) * Matrix.CreateScale(units, units, 1));
            try
            {
                draw(batch, new Rectangle(x, y, (int)MathF.Round(View.AbsoluteWidth * 2), (int)MathF.Round(View.AbsoluteHeight * 2)));
            }
            finally
            {
                batch.End();
                device.ScissorRectangle = scissor;
            }
        }
    }
}
