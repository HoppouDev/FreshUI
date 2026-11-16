using FreshUI.Hooks;
using Aether.UI.Game;
using FreshUI.Hooks.Mixins;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;

namespace FreshUI.Screens;

/// <summary>
/// Utility icons in slot-sized columns flanking the inventory. Before Gum is ready, the legacy
/// layout remains available.
/// </summary>
public static class SideButtons
{
    /// <summary>The right utility column follows the inventory's ammo column.</summary>
    public static bool TryOrigin(out Vector2 origin)
    {
        origin = default;
        if (!HudScreen.TrySlotPosition(2, 54, out Vector2 ammo))
            return false;
        origin = ammo + new Vector2(46, 0);
        return true;
    }
    /// <summary>
    /// Whether the game drew the banners' toggle this frame (there are banners to claim), set as it draws, before Gum
    /// draws the side buttons, and cleared after: the bestiary and emotes take its row when it's not there.
    /// </summary>
    public static bool BannerToggleShown;

    /// <summary>
    /// The top left of a cell (a slot's 44 units square) in the column right of the ammo (<paramref name="column"/> 0 and
    /// on, rightwards) or left of the coins (-1 and on, leftwards), on the inventory's rows (<paramref name="row"/> 0 its
    /// top, 4 the hotbar's).
    /// </summary>
    public static bool TryCell(int column, int row, out Vector2 cell)
    {
        cell = default;
        if (column >= 0 ? !TryOrigin(out Vector2 origin) : !HudScreen.TrySlotPosition(1, 50, out origin))
            return false;
        cell = origin + new Vector2(column * 46, row * 46);
        return true;
    }

    /// <summary>A toggle (drawn centred on x, y) at the middle of a side button cell (<see cref="SideButtons.TryCell"/>).</summary>
    public static void MoveToggle(ref int x, ref int y, int column, int row)
    {
        if (!SideButtons.TryCell(column, row, out Vector2 cell))
            return;
        x = (int)MathF.Round(cell.X + 22);
        y = (int)MathF.Round(cell.Y + 22);
    }

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
            // Left of the coins, under the nearby chests toggle: quick stack, then sort; right of the ammo, under the
            // crafting toggle and the banners' (while it shows): bestiary, then emotes.
            (int column, int row) = button == InventoryButtons.QuickStackAll.Button ? (-1, 1)
                : button == InventoryButtons.SortInventory.Button ? (-1, 2)
                : (0, (BannerToggleShown ? 2 : 1) + (button == InventoryButtons.Emotes.Button ? 1 : 0));
            if (TryCell(column, row, out Vector2 origin))
            {
                rectangle.X = (int)MathF.Round(origin.X + (44 - rectangle.Width) / 2);
                rectangle.Y = (int)MathF.Round(origin.Y + (44 - rectangle.Height) / 2);
                return rectangle;
            }
            // Centred under the 32-unit column, on even UI units.
            int left = (int)columnX + (32 - rectangle.Width) / 2;
            rectangle.X += left - (left & 1) - vanillaColumnX;
            rectangle.Y += ShiftY;
            return rectangle;
        };
    }

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
                int cell = HudScreen.IsReady ? 0 : 1;
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
            // Left of the coins under sort (its last two rows), then on down columns further left, a row each.
            (int column, int row) = index < 2 ? (-1, 3 + index) : (-2 - (index - 2) / 5, (index - 2) % 5);
            if (TryCell(column, row, out Vector2 origin))
            {
                float x = origin.X + (44 - drawn.X) / 2;
                float y = origin.Y + (44 - drawn.Y) / 2;
                return new Rectangle((int)MathF.Round(x), (int)MathF.Round(y), drawn.X, drawn.Y);
            }
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
