using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.UI.Gamepad;

namespace FreshUI.GumUi;

/// <summary>
/// The gamepad on the HUD (the inventory and everything open with it), by Gum's focus. Every control the HUD
/// presents places the game's gamepad point for it (<c>UILinkPointNavigator.SetPosition</c>: slots, buttons,
/// icons, recipes…), and each point placed this update gets an invisible focus target there, in one of Gum's
/// spatial-navigation zones: the D-pad and left stick move Gum's focus to the nearest target that way (the
/// game's own travel, which follows links laid out for the game's layout, is off), and the game's navigator goes
/// to the focused target's point. So the game's cursor follows the focus, and A, the slots' actions and the
/// instruction bar's hints stay the game's for that point. The crafting pages (9 to 11), which move through
/// recipes themselves, keep the game's travel, the focus following it.
/// </summary>
public static class GumHudFocus
{
    // The hotbar lock (a Gum button: the game's lock had no point) on a page of its own, valid with the inventory.
    private const int LockPage = 31300, LockPoint = 31300;
    private const float TargetSize = 8;

    // When each point was last placed (the game places a point where it draws its element).
    private static readonly Dictionary<int, uint> Placed = [];
    private static readonly Dictionary<int, Target> Targets = [];
    private static UILinkPage? _lockPage;
    private static InteractiveGue? _zone;
    private static readonly List<FrameworkElement> Shown = [];

    /// <summary>Whether the gamepad drives the HUD by Gum's focus (the inventory open, the gamepad in use).</summary>
    public static bool Active { get; private set; }

    /// <summary><c>UILinkPointNavigator.SetPosition</c>: the point is drawn this update.</summary>
    public static void Place(int point) => Placed[point] = Main.GameUpdateCount;

    private static bool IsLive(int point) =>
        Placed.TryGetValue(point, out uint at) && Main.GameUpdateCount - at <= 1 &&
        UILinkPointNavigator.Points.TryGetValue(point, out UILinkPoint? link) && (link.Page < 1000 || link.Page == LockPage);

    // The crafting pages move through recipes with the game's own links.
    private static bool MovesItself(int point) =>
        UILinkPointNavigator.Points.TryGetValue(point, out UILinkPoint? link) && link.Page is 9 or 10 or 11;

    /// <summary>The game's travel from a page (the D-pad, the stick): true while it's Gum's to do.</summary>
    public static bool TakesTravel(UILinkPage page) => Active && !MovesItself(page.CurrentPoint);

    /// <summary>Puts the hotbar lock's point at <paramref name="position"/> (UI units) this update.</summary>
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

    /// <summary>The HUD's zone of focus targets (a child of the HUD screen, over its canvas).</summary>
    public static InteractiveGue Zone => _zone ??= GumGamepad.SpatialZone();

    /// <summary>After each Gum update: whether the HUD takes the gamepad, its targets on this update's points, and the focus on the game's point.</summary>
    internal static void Update()
    {
        Active = !Main.gameMenu && Main.playerInventory && !Main.ingameOptionsWindow && !Main.inFancyUI &&
            PlayerInput.UsingGamepadUI && UILinkPointNavigator.InUse && _zone?.Parent != null;
        float scale = UiPixels.Scale;
        foreach ((int point, uint _) in Placed)
        {
            bool shown = Active && IsLive(point);
            if (!Targets.TryGetValue(point, out Target? target))
            {
                if (!shown)
                    continue;
                target = Targets[point] = new Target(point);
                _zone!.Children.Add(target.Visual);
            }
            if (target.Visual.Visible != shown)
                target.Visual.Visible = shown;
            if (!shown)
                continue;
            // The point is in screen pixels; Gum's canvas in UI pixels.
            Vector2 at = UILinkPointNavigator.Points[point].Position / scale;
            float x = MathF.Round(at.X - TargetSize / 2), y = MathF.Round(at.Y - TargetSize / 2);
            if (target.Visual.X != x || target.Visual.Y != y)
                (target.Visual.X, target.Visual.Y) = (x, y);
        }
        object? focused = InteractiveGue.CurrentInputReceiver;
        if (!Active)
        {
            foreach (Target target in Targets.Values)
            {
                if (target.IsFocused)
                    target.IsFocused = false;
            }
            return;
        }
        // The focus follows the game's point where the game moved it (opening a page, a recipe's craft slot…).
        if (Targets.TryGetValue(UILinkPointNavigator.CurrentPoint, out Target? current) && current.Visual.Visible && focused != current)
            Focus(current);
    }

    // As Gum's own moves: focusing an element doesn't clear the last one's focus, which then can't take it again.
    private static void Focus(Target target)
    {
        object? last = InteractiveGue.CurrentInputReceiver;
        target.IsFocused = true;
        if (last is FrameworkElement element && last != target)
            element.IsFocused = false;
    }

    /// <summary>A point's focus target: Gum's navigation moves onto it, and the game's navigator follows.</summary>
    private sealed class Target : FrameworkElement, IInputReceiver
    {
        private readonly int _point;

        public Target(int point) : base(new ContainerRuntime())
        {
            _point = point;
            Visual.HasEvents = true;
            Visual.WidthUnits = Visual.HeightUnits = DimensionUnitType.Absolute;
            Visual.Width = Visual.Height = TargetSize;
            GotFocus += (_, _) =>
            {
                if (UILinkPointNavigator.CurrentPoint != _point)
                    UILinkPointNavigator.ChangePoint(_point);
            };
        }

        IInputReceiver? IInputReceiver.ParentInputReceiver => null;

        void IInputReceiver.OnGainFocus() { }

        void IInputReceiver.OnLoseFocus() { }

        void IInputReceiver.OnFocusUpdatePreview(RoutedEventArgs args) { }

        void IInputReceiver.DoKeyboardAction(IInputReceiverKeyboard keyboard) { }

        // Only the moves, to the nearest shown target that way (Gum's spatial search): A is the game's click at its cursor, on this point.
        void IInputReceiver.OnFocusUpdate()
        {
            if (MovesItself(UILinkPointNavigator.CurrentPoint))
                return;
            foreach (IGamePad gamepad in GamePadsForUiControl)
            {
                float? angle = Direction(gamepad);
                if (angle == null)
                    continue;
                Shown.Clear();
                foreach (Target candidate in Targets.Values)
                {
                    if (candidate.Visual.Visible)
                        Shown.Add(candidate);
                }
                FrameworkElement? next = Gum.Forms.SpatialNavigationService.FindBestCandidate(this, angle.Value, Shown);
                if (next is Target target)
                    Focus(target);
                return;
            }
        }

        // The D-pad (repeating while held), else the left stick, as Gum's own navigation reads them: the angle, y down.
        private static float? Direction(IGamePad gamepad)
        {
            if (gamepad.ButtonRepeatRate(GamepadButton.DPadRight))
                return 0;
            if (gamepad.ButtonRepeatRate(GamepadButton.DPadDown))
                return MathF.PI / 2;
            if (gamepad.ButtonRepeatRate(GamepadButton.DPadLeft))
                return MathF.PI;
            if (gamepad.ButtonRepeatRate(GamepadButton.DPadUp))
                return -MathF.PI / 2;
            if (gamepad.LeftStick.RadialPushedRepeatRate() && (gamepad.LeftStick.X != 0 || gamepad.LeftStick.Y != 0))
                return MathF.Atan2(-gamepad.LeftStick.Y, gamepad.LeftStick.X);
            return null;
        }
    }
}
