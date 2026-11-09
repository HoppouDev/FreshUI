using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI;

/// <summary>Centres an equipment page icon (camera, housing, equipment) over its column of the page.</summary>
public static class PageIcons
{
    public static void Centre(UIIconButton button)
    {
        Func<Rectangle> area = button.Area;
        button.Area = () =>
        {
            Rectangle rectangle = area();
            // Vanilla's: screenWidth - 80 - 48 × index, drawn at 0.9 from the left.
            int index = (int)MathF.Round((Main.screenWidth - 80 - rectangle.X) / 48f);
            int columnLeft = Main.screenWidth - 92 - (int)PixelSlots.Pitch * index;
            float centre = columnLeft - (columnLeft & 1) + PixelSlots.Size(Hud.SlotScale) / 2f;
            int left = (int)MathF.Round(centre - rectangle.Width * Sharp.WholeScale(0.9f) / 2f);
            rectangle.X = left - (left & 1);
            return rectangle;
        };
    }
}
