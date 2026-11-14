using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI.Gamepad;

namespace FreshUI.Legacy;

/// <summary>
/// The gamepad in the inventory by where things are (<c>UILinkPointNavigatorMixin</c>, <c>UILinkPageMixin</c>):
/// the game links its inventory's points (slots, coins and ammo, equipment, buttons, chests, crafting) for
/// its own layout, which FreshUI has moved, so each move of the D-pad goes to the nearest point drawn this
/// frame in that direction, on any of the game's in-game pages (the navigator changes page with the
/// point); preferably one in line with it, else the nearest within a cone; with none that way, the focus
/// stays (the game's link would go where it laid things out). From the crafting pages (9 to 11), which move
/// through recipes themselves, and from points not drawn this frame, the game's own links.
/// </summary>
public static class InventoryNavigation
{
    // When each point was last placed (the game places a point where it draws its element).
    private static readonly Dictionary<int, uint> Placed = [];

    /// <summary><c>UILinkPointNavigator.SetPosition</c>: the point is drawn this frame.</summary>
    public static void Place(int point) => Placed[point] = Main.GameUpdateCount;

    private static bool IsLive(int point) =>
        Placed.TryGetValue(point, out uint at) && Main.GameUpdateCount - at <= 1 &&
        UILinkPointNavigator.Points.TryGetValue(point, out UILinkPoint? link) && (link.Page < 1000 || link.Page == LockPage);

    // The hotbar lock (a Gum button, FreshUI's: the game's lock had no point) on a page of its own, valid with the inventory.
    private const int LockPage = 31300, LockPoint = 31300;
    private static UILinkPage? _lockPage;

    /// <summary>Puts the hotbar lock's point at <paramref name="position"/> (UI units) this frame.</summary>
    public static void PlaceHotbarLock(Vector2 position)
    {
        if (_lockPage == null)
        {
            _lockPage = new UILinkPage();
            _lockPage.LinkMap.Add(LockPoint, new UILinkPoint(LockPoint, enabled: true, -3, -4, -1, -2));
            _lockPage.IsValidEvent += () => Main.playerInventory && !Main.gameMenu;
            _lockPage.CanEnterEvent += () => Main.playerInventory && !Main.gameMenu;
            UILinkPointNavigator.RegisterPage(_lockPage, LockPage);
        }
        UILinkPointNavigator.SetPosition(LockPoint, position);
    }

    /// <summary>Whether the gamepad is on the hotbar lock.</summary>
    public static bool OnHotbarLock => UILinkPointNavigator.InUse && UILinkPointNavigator.CurrentPage == LockPage;

    /// <summary>A move from the page's point (<paramref name="direction"/>: a unit vector, y down); true if it's handled here.</summary>
    public static bool Travel(UILinkPage page, Vector2 direction)
    {
        if (Main.gameMenu || !Main.playerInventory || Main.ingameOptionsWindow || Main.inFancyUI)
            return false;
        int from = page.CurrentPoint;
        // The crafting pages move through recipes themselves.
        if (!IsLive(from) || UILinkPointNavigator.Points[from].Page is 9 or 10 or 11)
            return false;
        Vector2 origin = UILinkPointNavigator.Points[from].Position;
        // In line: within half a slot across (a slot is about 44 UI units, at the UI's scale).
        float line = 22f * Main.UIScale;
        int best = -1;
        float bestScore = float.MaxValue;
        foreach ((int point, uint _) in Placed)
        {
            if (point == from || !IsLive(point))
                continue;
            Vector2 offset = UILinkPointNavigator.Points[point].Position - origin;
            float along = Vector2.Dot(offset, direction);
            float across = MathF.Abs(direction.X != 0 ? offset.Y : offset.X);
            if (along < 4f)
                continue;
            bool lined = across <= line;
            if (!lined && across > along * 1.2f)
                continue;
            float score = (lined ? 0 : 100000) + along + across * 2;
            if (score < bestScore)
            {
                bestScore = score;
                best = point;
            }
        }
        // Nothing that way: stay (the game's link would go to a point it laid out elsewhere, or isn't drawn).
        if (best >= 0)
            UILinkPointNavigator.ChangePoint(best);
        return true;
    }
}
