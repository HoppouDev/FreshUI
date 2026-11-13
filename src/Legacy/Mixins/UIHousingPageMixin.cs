using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The housing page's NPC slots on the art's pixel grid, 46 apart down a column and between columns
/// (vanilla's 56 × 0.85, truncated, and 48), so they have one art pixel between them.
/// </summary>
[Mixin(typeof(UIHousingPage))]
public class UIHousingPageMixin
{
#pragma warning disable CS0649
    [Shadow]
    private int _top;
#pragma warning restore CS0649

    private const float VanillaRowPitch = 56f * 0.85f;

    private const float VanillaColumnPitch = 48f;

    [Redirect("UpdateState()", At.INVOKE, Target = "Aether.UI.Game.UIHousingIcon::Set(System.Int32,System.Int32,System.Int32,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2)")]
    private void PlaceIcon(UIHousingIcon icon, int head, int npc, int index, Vector2 position, Vector2 size)
    {
        // Columns go leftwards from vanilla's right edge (the equipment page's first column).
        int columnX = Main.screenWidth - 64 - 28;
        int column = (int)MathF.Round((columnX - position.X) / VanillaColumnPitch);
        int row = (int)MathF.Round((position.Y - _top) / VanillaRowPitch);
        int x = columnX - column * (int)PixelSlots.Pitch;
        int y = _top + row * (int)PixelSlots.Pitch;
        icon.Set(head, npc, index, new Vector2(x - (x & 1), y - (y & 1)), size);
    }
}
