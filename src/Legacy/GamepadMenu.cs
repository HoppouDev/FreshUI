using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Initializers;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Legacy;

/// <summary>
/// FreshUI's in-game menus (the pause menu, the settings window) on the game's gamepad navigation, as
/// one link page of their own: each frame the open menu lists what can be focused, and each item links
/// to its nearest neighbour in each direction, by where it is on screen. The game's navigator moves the
/// (virtual) cursor onto the focused item, so hovering and clicking (A) work as with a mouse.
/// <para>
/// A slider item takes left and right itself: the D-pad steps it, the left stick (the game's own
/// handling) slides it. LT and RT go to the previous and next tab; moving past the list's first or last
/// row scrolls it. The page's hints are shown in the game's instruction bar.
/// </para>
/// </summary>
public static class GamepadMenu
{
    // The page's ID and its points' (the game's own run to 30000).
    private const int PageId = 31000;
    private const int FirstPoint = 31000;
    private const int Capacity = 64;

    private static UILinkPage? _page;
    private static readonly List<Item> Building = [];
    private static Item[] _items = [];
    private static uint _publishedAt;
    private static object? _owner;
    // Each menu's last focus while the menus stay open: back from the settings to the pause menu, the
    // focus is where it was (Settings), not at the start.
    private static readonly Dictionary<object, int> LastFocus = [];
    private static Func<int, string>? _hints;
    private static Action<int>? _switchTab;
    private static Action<int>? _scroll;

    private readonly record struct Item(Vector2 Position, Rectangle Box, Action<int>? Adjust, int Left, int Right);

    /// <summary>The focused item's index in the last published list, or -1.</summary>
    public static int Focused => _page != null && UILinkPointNavigator.CurrentPage == PageId
        ? UILinkPointNavigator.CurrentPoint - FirstPoint : -1;

    /// <summary>Whether a gamepad is driving the menu (the game's navigator is on this page).</summary>
    public static bool Active => UILinkPointNavigator.InUse && UILinkPointNavigator.CurrentPage == PageId;

    /// <summary>
    /// Adds an item to this frame's list; <paramref name="adjust"/> (−1 or 1) makes it a slider.
    /// <paramref name="left"/> and <paramref name="right"/> are the items (by index) left and right go to
    /// in place of the nearest, as between a list of tabs and the open tab's rows.
    /// </summary>
    public static void Add(UIElement element, Action<int>? adjust = null, int left = -1, int right = -1)
    {
        if (Building.Count >= Capacity)
            return;
        CalculatedStyle box = element.GetDimensions();
        Building.Add(new Item(box.Center(), new Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height), adjust, left, right));
    }

    /// <summary>How many items this frame's list has so far: the next one's index.</summary>
    public static int Count => Building.Count;

    /// <summary>
    /// Publishes this frame's items (from <see cref="Add"/>) for <paramref name="owner"/> (the menu; when
    /// it changes, the focus starts at <paramref name="start"/>): <paramref name="hints"/> gives the
    /// instruction bar's text for the focused item's index, <paramref name="switchTab"/> takes LT/RT
    /// (−1, 1), <paramref name="scroll"/> moving past the first or last item (−1, 1).
    /// </summary>
    public static void Publish(object owner, Func<int, string> hints, int start = 0, Action<int>? switchTab = null, Action<int>? scroll = null)
    {
        Register();
        // Reopened: every menu's focus starts afresh.
        if (!IsPublished)
        {
            LastFocus.Clear();
            _owner = null;
        }
        _items = Building.ToArray();
        Building.Clear();
        _hints = hints;
        _switchTab = switchTab;
        _scroll = scroll;
        _publishedAt = Main.GameUpdateCount;
        for (int i = 0; i < _items.Length; i++)
        {
            UILinkPointNavigator.SetPosition(FirstPoint + i, _items[i].Position);
        }
        Link();
        if (UILinkPointNavigator.CurrentPage != PageId && _items.Length > 0)
            UILinkPointNavigator.ChangePage(PageId);
        // Another menu: its own first focus, and the cursor straight onto it (the navigator only moves
        // the cursor as the gamepad moves).
        if (owner != _owner && _items.Length > 0)
        {
            if (_owner != null && Focused >= 0)
                LastFocus[_owner] = Focused;
            _owner = owner;
            Focus(LastFocus.TryGetValue(owner, out int last) ? last : start);
        }
    }

    /// <summary>Focuses the published item at <paramref name="index"/>, the cursor straight onto it.</summary>
    public static void Focus(int index)
    {
        if (_page == null || _items.Length == 0)
            return;
        index = Math.Clamp(index, 0, _items.Length - 1);
        _page.CurrentPoint = FirstPoint + index;
        if (Active)
        {
            Vector2 at = UILinkPointNavigator.Points[FirstPoint + index].Position;
            Terraria.GameInput.PlayerInput.MouseX = (int)at.X;
            Terraria.GameInput.PlayerInput.MouseY = (int)at.Y;
        }
    }

    private static bool IsPublished =>
        _items.Length > 0 && Main.GameUpdateCount - _publishedAt <= 2 && Main.ingameOptionsWindow && !Main.InGameUI.IsVisible;

    private static void Register()
    {
        if (_page != null)
            return;
        _page = new UILinkPage();
        for (int i = 0; i < Capacity; i++)
        {
            _page.LinkMap.Add(FirstPoint + i, new UILinkPoint(FirstPoint + i, enabled: true, -3, -4, -1, -2));
        }
        _page.IsValidEvent += () => IsPublished;
        _page.CanEnterEvent += () => IsPublished;
        _page.EnterEvent += () => _page.CurrentPoint = FirstPoint;
        _page.UpdateEvent += () =>
        {
            if (Focused < 0 || Focused >= _items.Length)
                _page.CurrentPoint = FirstPoint;
            // The left stick slides a focused slider (the game's handling, for the slider's feature).
            if (_items.Length > 0 && _items[Focused].Adjust != null)
                UILinksInitializer.HandleOptionsSpecials();
        };
        // No neighbour that way: a slider steps, the list scrolls.
        _page.ReachEndEvent += (_, direction) =>
        {
            int index = Focused;
            if (index < 0 || index >= _items.Length)
                return;
            if (direction is -3 or -4 && _items[index].Adjust is Action<int> adjust)
                adjust(direction == -3 ? -1 : 1);
            else if (direction is -1 or -2)
                _scroll?.Invoke(direction == -1 ? -1 : 1);
        };
        _page.OnPageMoveAttempt += direction => _switchTab?.Invoke(direction);
        _page.OnSpecialInteracts += () => _hints?.Invoke(Focused) ?? "";
        UILinkPointNavigator.RegisterPage(_page, PageId);
    }

    // Each item to its nearest neighbour in each direction: preferably one it lines up with (their
    // boxes overlap across the direction), else the nearest within a narrow cone. None: the page's
    // edge (−3 left, −4 right, −1 up, −2 down), which a slider takes for its steps.
    private static void Link()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            UILinkPoint point = _page!.LinkMap[FirstPoint + i];
            point.Left = _items[i].Adjust != null ? -3 : Given(_items[i].Left) ?? Nearest(i, -Vector2.UnitX, -3);
            point.Right = _items[i].Adjust != null ? -4 : Given(_items[i].Right) ?? Nearest(i, Vector2.UnitX, -4);
            point.Up = Nearest(i, -Vector2.UnitY, -1);
            point.Down = Nearest(i, Vector2.UnitY, -2);
        }
    }

    private static int? Given(int index) => index >= 0 && index < _items.Length ? FirstPoint + index : null;

    private static int Nearest(int from, Vector2 direction, int edge)
    {
        Item origin = _items[from];
        int best = edge;
        float bestScore = float.MaxValue;
        for (int i = 0; i < _items.Length; i++)
        {
            if (i == from)
                continue;
            Vector2 offset = _items[i].Position - origin.Position;
            float along = Vector2.Dot(offset, direction);
            float across = MathF.Abs(direction.X != 0 ? offset.Y : offset.X);
            if (along <= 1)
                continue;
            bool lined = direction.X != 0
                ? _items[i].Box.Top < origin.Box.Bottom && origin.Box.Top < _items[i].Box.Bottom
                : _items[i].Box.Left < origin.Box.Right && origin.Box.Left < _items[i].Box.Right;
            if (!lined && across > along * 0.4f)
                continue;
            float score = (lined ? 0 : 100000) + along + across * 0.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = FirstPoint + i;
            }
        }
        return best;
    }

    /// <summary>An instruction bar entry: the glyphs of the gamepad UI's bindings for <paramref name="trigger"/>, then <paramref name="text"/>.</summary>
    public static string Hint(string text, string trigger) =>
        Terraria.GameInput.PlayerInput.BuildCommand(text, Terraria.GameInput.PlayerInput.ProfileGamepadUI.KeyStatus[trigger]);

    /// <summary>An instruction bar entry for fixed buttons (e.g. "DPadLeft", "DPadRight").</summary>
    public static string HintButtons(string text, params string[] buttons) =>
        Terraria.GameInput.PlayerInput.BuildCommand(text, new List<string>(buttons));
}
