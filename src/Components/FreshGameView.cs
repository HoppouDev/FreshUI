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

        // Its own batch key: Gum ends the batch it was drawing in before this draws, so what Gum drew
        // earlier stays under the game's content and what it draws later goes over it.
        public override string BatchKey => "FreshUI.GameView";

        public override void Render(ISystemManagers managers)
        {
            if (View?.Draw is not { } draw || !View.AbsoluteVisible)
                return;
            float units = UiPixels.Scale / 2f;
            SpriteBatch batch = Main.spriteBatch;
            batch.Begin(SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, FreshUI.Legacy.Sharp.Snapped, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Matrix.CreateScale(units, units, 1));
            try
            {
                draw(batch, new Rectangle((int)MathF.Round(View.AbsoluteLeft * 2), (int)MathF.Round(View.AbsoluteTop * 2),
                    (int)MathF.Round(View.AbsoluteWidth * 2), (int)MathF.Round(View.AbsoluteHeight * 2)));
            }
            finally
            {
                batch.End();
            }
        }
    }
}
