using Aether.UI.Game;
using FreshUI.Mixins;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;

namespace FreshUI;

/// <summary>
/// The grid of inventory side buttons beside the inventory, where vanilla's coin and ammo columns were
/// (FreshUI moved them under it): two 32-unit columns 34 apart, under the achievement advisor's card.
/// Vanilla's quick stack, sort, bestiary and emote buttons start it, and the icons vanilla keeps at the
/// screen's left edge (the hotbar lock and the builder toggles: block replacement, torch swapping and
/// the builder accessories') are packed in below. With a chest or shop open, the buttons stay beside
/// it, as vanilla's, a row lower with it.
/// </summary>
public static class SideButtons
{
    /// <summary>The grid's first column's left edge (one art pixel right of the inventory's last column).</summary>
    public const float ColumnX = 482f;

    /// <summary>The grid's columns' and rows' pitch.</summary>
    public const float Pitch = 34f;

    // Vanilla's quick stack and sort row; its bestiary and emote row is 34 lower.
    private const int VanillaTop = 244;

    /// <summary>
    /// The grid's top with no chest open: two UI units under the advisor's card (its 72-texel border at
    /// its whole-pixel scale, from the inventory's top), on even UI units.
    /// </summary>
    public static int Top
    {
        get
        {
            int cardBottom = (int)MathF.Ceiling(20f + 72f * Sharp.WholeScale(0.75f));
            return cardBottom + (cardBottom & 1) + 4;
        }
    }

    // How far a vanilla side button moves down.
    private static int ShiftY => Main.ChestOrShopUIVisible ? MainMixin.ChestDrop : Top - VanillaTop;

    /// <summary>Moves a side button from under one of vanilla's columns (x 498 or 534) to one of the grid's.</summary>
    public static void Move(UIIconButton button, int vanillaColumnX, float columnX)
    {
        Func<Rectangle> area = button.Area;
        button.Area = () =>
        {
            Rectangle rectangle = area();
            // Centred under the 32-unit column, on even UI units.
            int left = (int)columnX + (32 - rectangle.Width) / 2;
            rectangle.X += left - (left & 1) - vanillaColumnX;
            rectangle.Y += ShiftY;
            return rectangle;
        };
    }

    /// <summary>
    /// Puts the hotbar lock in the grid's first free cell, under the bestiary button (it's drawn at 0.9
    /// from its top left).
    /// </summary>
    public static void PlaceHotbarLock(UIIconButton button) => Place(button, () => 0, _ => ScaledFrame(TextureAssets.HbLock[0].Value.Frame(2)));

    /// <summary>
    /// Puts the builder toggles' buttons in the grid after the hotbar lock, packed in order, skipping the
    /// hidden ones: block replacement and torch swapping (drawn at 0.9 from their top left), then the
    /// accessory toggles (the group's first ten, drawn centred).
    /// </summary>
    public static void PlaceBuilderToggles(UIBuilderToggles group)
    {
        int[] order = new int[12] { 10, 11, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Func<Rectangle>[] areas = new Func<Rectangle>[order.Length];
        for (int i = 0; i < order.Length; i++)
            areas[i] = group.Buttons[order[i]].Area;
        for (int i = 0; i < order.Length; i++)
        {
            int index = i;
            Func<Rectangle, Point> size = order[i] switch
            {
                10 => _ => ScaledFrame(TextureAssets.blockReplaceIcon[0].Value.Frame(3)),
                11 => _ => ScaledFrame(TextureAssets.Extra[211].Value.Frame(4)),
                _ => rectangle => new Point(rectangle.Width, rectangle.Height)
            };
            Place(group.Buttons[order[i]], () =>
            {
                int cell = 1;
                for (int j = 0; j < index; j++)
                {
                    if (!areas[j]().IsEmpty)
                        cell++;
                }
                return cell;
            }, size);
        }
    }

    private static Point ScaledFrame(Rectangle frame)
    {
        float scale = Sharp.WholeScale(0.9f);
        return new Point((int)MathF.Ceiling(frame.Width * scale), (int)MathF.Ceiling(frame.Height * scale));
    }

    // Cells are two to a row, under the coin and ammo columns, from the row under the bestiary and emote
    // buttons on down; they follow that row while a chest or shop is open. Each button is centred in
    // its cell (32 wide, as the columns, and 30 tall, as the buttons above), on even UI units.
    private static void Place(UIIconButton button, Func<int> cell, Func<Rectangle, Point> size)
    {
        Func<Rectangle> area = button.Area;
        button.Area = () =>
        {
            Rectangle rectangle = area();
            if (rectangle.IsEmpty)
                return rectangle;
            Point drawn = size(rectangle);
            int index = cell();
            // Vanilla's bestiary row: 30 tall from 278, 168 lower (and 5 right) with a chest or shop
            // open, 24 more while renaming a chest; moved as the buttons are.
            bool chest = Main.ChestOrShopUIVisible;
            float cellX = ColumnX + Pitch * (index % 2) + (chest ? 5 : 0);
            float cellY = VanillaTop + Pitch + (chest ? 168 : 0) + (Main.editChest ? 24 : 0) + ShiftY + Pitch * (1 + index / 2);
            int left = (int)MathF.Round(cellX + (32 - drawn.X) / 2f);
            int top = (int)MathF.Round(cellY + (30 - drawn.Y) / 2f);
            return new Rectangle(left - (left & 1), top - (top & 1), drawn.X, drawn.Y);
        };
    }
}
