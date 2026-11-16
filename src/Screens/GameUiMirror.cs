using System.Reflection;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI.Gamepad;
using Terraria.GameInput;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// A game screen (a <see cref="UIElement"/> tree the game built and laid out: its own handlers, texts and state) drawn
/// and operated by Gum: each element gets a FreshUI component at its laid-out place, in the tree's order. Option
/// buttons (<c>GroupOptionButton</c>, with their icon, chosen while selected), text panels, name plates and any
/// element the game listens to for clicks are <see cref="FreshPanelButton"/>s; panels <see cref="FreshPanel"/>s,
/// separators <see cref="FreshSeparator"/>s and texts Gum text in the game's size and colour; other leaves (previews,
/// portraits) are drawn by the game in a <see cref="FreshGameView"/>. Gum's clicks (the mouse, A on the gamepad's
/// focus, automation) reach the element as the game's mouse events (down, up, click: its handlers run as before), and
/// hovering or focusing it as its mouse over and out (descriptions). The screen sets <see cref="Source"/> and calls
/// <see cref="Update"/> each update; the game's state itself is routed off (no update or drawing of its own).
/// </summary>
internal sealed class GameUiMirror : ContainerRuntime
{
    // The game's fonts' sizes at scale 1, in UI units an em, and their baselines below a line's top (SdfText).
    private const float MouseTextEm = 19, MouseTextBaseline = 15.75f, DeathTextEm = 49.5f, DeathTextBaseline = 39.88f;

    private static readonly FieldInfo MouseDownEvent = typeof(UIElement).GetField("OnLeftMouseDown", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ClickEvent = typeof(UIElement).GetField("OnLeftClick", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo TextScale = typeof(UIText).GetField("_textScale", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo TextLarge = typeof(UIText).GetField("_isLarge", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly InteractiveGue _zone = GumGamepad.SpatialZone();
    private readonly List<Mirror> _mirrors = [];
    private UIElement? _built;
    private int _builtShape;
    private Point _laidOutFor;

    public GameUiMirror()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(_zone);
    }

    /// <summary>The game's screen to show.</summary>
    public UIElement? Source { get; set; }

    /// <summary>
    /// Whether the game's cursor drives the controls while the gamepad's in use (on the HUD, where the game's navigator
    /// moves it along the game's own gamepad points): the control under it is hovered as the mouse would (lit, the
    /// element's mouse over), and A (the game's left click there) clicks it.
    /// </summary>
    public bool FollowsGameCursor { get; set; }

    private Mirror? _underCursor;

    /// <summary>
    /// Whether its panels take the mouse, so clicks on them don't reach a world behind (on the HUD; a menu's backdrop
    /// already takes them). The game's hover handlers that stopped them run only for controls here.
    /// </summary>
    public bool PanelsTakeMouse { get; init; }

    /// <summary>The first control (the gamepad's focus starts there), if any.</summary>
    public FreshPanelButton? First => _mirrors.Select(m => m.Button).FirstOrDefault(b => b is { IsEnabled: true });

    /// <summary>Each update: rebuilt when the game's tree changes, then each component on its element.</summary>
    public void Update()
    {
        if (Source == null)
            return;
        // The game lays its screen out again when the screen's size changes (its interface would have).
        var size = new Point(Main.screenWidth, Main.screenHeight);
        if (size != _laidOutFor)
        {
            _laidOutFor = size;
            Source.Recalculate();
        }
        // The game's own per-update work for its elements (their OnUpdate), as its interface's update would.
        Source.Update(Main.gameTimeCache);
        int shape = Shape(Source);
        if (Source != _built || shape != _builtShape)
        {
            if (Source != _built)
                _known.Clear();
            _built = Source;
            // Elements the game added since the last build (a category's) are laid out first; only those: an element's
            // own Recalculate can do more (the bestiary's grid refills itself with new entries, the screen drops its
            // selection), and laying out what was already there would change the tree again every update.
            foreach (UIElement child in Source.Children)
                LayOutNew(child);
            _known.Clear();
            Remember(Source);
            _builtShape = Shape(Source);
            Build();
        }
        foreach (Mirror mirror in _mirrors)
            mirror.Sync();
        if (FollowsGameCursor)
            FollowGameCursor();
    }

    private void FollowGameCursor()
    {
        Mirror? under = null;
        if (GamepadCursor() is { } cursor)
        {
            // The cursor in UI units; Gum's canvas is in UI pixels.
            float x = cursor.X / 2, y = cursor.Y / 2;
            for (int i = _mirrors.Count - 1; i >= 0 && under == null; i--)
            {
                GraphicalUiElement visual = _mirrors[i].Visual;
                if (_mirrors[i] is (ButtonMirror or HitMirror) && GumHost.IsShown(visual) &&
                    x >= visual.AbsoluteLeft && x < visual.AbsoluteLeft + visual.GetAbsoluteWidth() &&
                    y >= visual.AbsoluteTop && y < visual.AbsoluteTop + visual.GetAbsoluteHeight())
                    under = _mirrors[i];
            }
        }
        Vector2 at = GamepadCursor() ?? new Vector2(Main.mouseX, Main.mouseY);
        if (under != _underCursor)
        {
            if (_underCursor != null)
            {
                _underCursor.Element.MouseOut(new UIMouseEvent(_underCursor.Element, at));
                if (_underCursor.Button != null)
                    _underCursor.Button.IsLit = false;
            }
            if (under != null)
            {
                under.Element.MouseOver(new UIMouseEvent(under.Element, at));
                if (under.Button != null)
                    under.Button.IsLit = true;
            }
            _underCursor = under;
        }
        if (under != null && PlayerInput.Triggers.JustPressed.MouseLeft)
            ((InteractiveGue)under.Visual).CallClick();
    }

    /// <summary>
    /// The gamepad's cursor in UI units while the game's navigator drives it (its current point, kept in screen
    /// pixels); the game points its mouse there only while it draws its own UI, not when this runs or draws.
    /// </summary>
    private static Vector2? GamepadCursor() =>
        PlayerInput.UsingGamepadUI && UILinkPointNavigator.InUse && UILinkPointNavigator.Points.TryGetValue(UILinkPointNavigator.CurrentPoint, out UILinkPoint? point)
            ? point.Position / Main.UIScale : null;

    // The elements of the last build (by identity).
    private readonly HashSet<UIElement> _known = new(ReferenceEqualityComparer.Instance);

    private void LayOutNew(UIElement element)
    {
        if (!_known.Contains(element))
        {
            element.Recalculate();
            return;
        }
        foreach (UIElement child in element.Children)
            LayOutNew(child);
    }

    private void Remember(UIElement element)
    {
        _known.Add(element);
        foreach (UIElement child in element.Children)
            Remember(child);
    }

    // The tree's elements, by identity and order: it changes when the game adds, removes or replaces elements (a
    // bestiary page's entries are all new buttons, as many as before).
    // Every update: the element list itself (UIElement.Children is an IEnumerable, boxing its enumerator per element).
    private static int Shape(UIElement element)
    {
        int shape = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(element);
        foreach (UIElement child in Elements(element))
            shape = shape * 31 + Shape(child);
        return shape;
    }

    private static readonly Func<UIElement, List<UIElement>> Elements = CompileElements();

    private static Func<UIElement, List<UIElement>> CompileElements()
    {
        var element = System.Linq.Expressions.Expression.Parameter(typeof(UIElement));
        FieldInfo field = typeof(UIElement).GetField("Elements", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return System.Linq.Expressions.Expression.Lambda<Func<UIElement, List<UIElement>>>(System.Linq.Expressions.Expression.Field(element, field), element).Compile();
    }

    private readonly Dictionary<(UIElement, Type), Mirror> _previous = [];
    // Each container's components in the new build's order.
    private readonly Dictionary<GraphicalUiElement, List<GraphicalUiElement>> _wanted = [];

    private void Build()
    {
        // _underCursor is kept: a reused mirror is the same control, and one that went is unlit as the cursor moves on.
        _previous.Clear();
        foreach (Mirror mirror in _mirrors)
            _previous.TryAdd((mirror.Element, mirror.GetType()), mirror);
        _mirrors.Clear();
        _wanted.Clear();
        _wanted[_zone] = [];
        foreach (UIElement child in Source!.Children)
            Add(child, _zone, Zero);
        _previous.Clear();
        foreach ((GraphicalUiElement container, List<GraphicalUiElement> wanted) in _wanted)
            Reconcile(container, wanted);
        _wanted.Clear();
    }

    // Only what changed is removed, added or moved: Gum counts a control taken out of the tree and put back as the
    // cursor leaving it (a roll-off, a press dropped), and a hover that changes the game's tree (a bestiary entry's
    // borders) would then rebuild every update.
    private static void Reconcile(GraphicalUiElement container, List<GraphicalUiElement> wanted)
    {
        var keep = new HashSet<GraphicalUiElement>(wanted);
        for (int i = container.Children.Count - 1; i >= 0; i--)
        {
            if (container.Children[i] is GraphicalUiElement child && !keep.Contains(child))
                container.Children.RemoveAt(i);
        }
        for (int i = 0; i < wanted.Count; i++)
        {
            GraphicalUiElement visual = wanted[i];
            if (i < container.Children.Count && container.Children[i] == visual)
                continue;
            if (visual.Parent is GraphicalUiElement parent)
                parent.Children.Remove(visual);
            container.Children.Insert(Math.Min(i, container.Children.Count), visual);
        }
    }

    private static readonly Func<Vector2> Zero = () => Vector2.Zero;
    private static readonly FieldInfo InnerList = typeof(UIList).GetField("_innerList", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly Dictionary<Type, bool> OverridesClicks = [];

    // Whether the game acts on clicks on the element: a handler on it, or its type's own mouse down or click.
    // Elements that act on the game's mouse themselves while hovered (an item slot, an item grid): Gum's hover reaches them as the game's.
    private static bool TakesHover(UIElement element) => element is UIItemSlot or UIDynamicItemCollection;

    // A place to click for each element under one the game draws whole that takes input.
    private void AddHits(UIElement element, GraphicalUiElement container, Func<Vector2> origin)
    {
        foreach (UIElement child in element.Children)
        {
            if (Listened(child) || TakesHover(child))
                Keep(new HitMirror(child), container, origin);
            AddHits(child, container, origin);
        }
    }

    private static bool Listened(UIElement element)
    {
        if (MouseDownEvent.GetValue(element) != null || ClickEvent.GetValue(element) != null)
            return true;
        Type type = element.GetType();
        if (!OverridesClicks.TryGetValue(type, out bool overrides))
        {
            overrides = type.GetMethod(nameof(UIElement.LeftMouseDown))!.DeclaringType != typeof(UIElement) ||
                type.GetMethod(nameof(UIElement.LeftClick))!.DeclaringType != typeof(UIElement);
            OverridesClicks[type] = overrides;
        }
        return overrides;
    }

    // An element and what's in it, in a container whose origin is at the given game position (UI units).
    private void Add(UIElement element, GraphicalUiElement container, Func<Vector2> origin, ListMirror? list = null)
    {
        Type type = element.GetType();
        bool listened = Listened(element) || TakesHover(element);
        if (element is UIScrollbar)
            return; // A list's own (its mirror scrolls).
        if (element is UIList gameList)
        {
            var mirror = (ListMirror)Keep(new ListMirror(gameList), container, origin);
            var inner = (UIElement)InnerList.GetValue(gameList)!;
            Func<Vector2> innerOrigin = () => inner.GetDimensions().Position();
            foreach (UIElement item in inner.Children)
                Add(item, mirror.Items, innerOrigin, mirror);
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GroupOptionButton<>))
            Keep(new OptionMirror(element), container, origin);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UITextPanel<>))
            Keep(new TextPanelMirror((UIPanel)element, listened), container, origin);
        else if (element is UITextPanel<string> textBox && type.Name == "UITextBox")
            Keep(new TextBoxMirror(textBox), container, origin);
        else if (element is UIDifficultyButton difficulty)
            Keep(new DifficultyMirror(difficulty), container, origin);
        else if (element is UIColoredImageButton imageButton)
            Keep(new ImageButtonMirror(imageButton), container, origin);
        else if (element is UIColoredSlider slider)
        {
            Keep(new SliderMirror(slider), container, origin);
            return;
        }
        else if (element is UICharacterNameButton || listened && element is UIPanel)
            Keep(new ButtonMirror(element, ""), container, origin);
        else if (element is UIPanel panel)
            Keep(new PanelMirror(element, () => panel.BackgroundColor, () => panel.BorderColor, PanelsTakeMouse), container, origin);
        else if (element is UISlicedImage image)
            Keep(new PanelMirror(element, () => image.Color, takesMouse: PanelsTakeMouse), container, origin);
        else if (element is UIHorizontalSeparator)
            Keep(new SeparatorMirror(element), container, origin);
        else if (element is UIText text)
            Keep(new TextMirror(text), container, origin);
        else if (element is UIImageButton labelled && element.Children.OfType<UIText>().FirstOrDefault() is { } label)
        {
            // A button of the game's art with a label on it (the bestiary's sorting and filtering): the menus' button
            // with the label. Its art has a texel per pixel, so its glyph can't be both its size and whole art pixels.
            Keep(new LabelledImageButtonMirror(labelled, label), container, origin);
            return;
        }
        else if (element is UIImage or UIImageFramed or UIImageButton && element.Children.Any())
        {
            // Art with something on it (the research slot's frame, the pistons and their sparks): all the game's
            // drawing, with a place to click for what on it takes input.
            Keep(new GameMirror(element, list, this), container, origin);
            if (listened)
                Keep(new HitMirror(element), container, origin);
            AddHits(element, container, origin);
            return;
        }
        else if (!element.Children.Any() && type != typeof(UIElement))
        {
            // The game's own art (a preview, a portrait…); a control too where the game listens to it.
            Keep(new GameMirror(element, list, this), container, origin);
            if (listened)
                Keep(new HitMirror(element), container, origin);
            return;
        }
        else if (listened)
            Keep(new HitMirror(element), container, origin);
        foreach (UIElement child in element.Children)
            Add(child, container, origin, list);
    }

    // The component kept for the element: the last build's, if it had one of this kind (so a control being pressed
    // survives a rebuild, as a bestiary entry's hover border coming and going), else the new one.
    private Mirror Keep(Mirror mirror, GraphicalUiElement container, Func<Vector2> origin)
    {
        if (_previous.Remove((mirror.Element, mirror.GetType()), out Mirror? last))
            mirror = last;
        mirror.Origin = origin;
        _mirrors.Add(mirror);
        _wanted[container].Add(mirror.Visual);
        if (mirror is ListMirror list)
            _wanted[list.Items] = [];
        mirror.Place();
        return mirror;
    }

    /// <summary>A component standing for a game element.</summary>
    private abstract class Mirror(UIElement element)
    {
        public readonly UIElement Element = element;
        public Func<Vector2> Origin = Zero;
        public abstract GraphicalUiElement Visual { get; }
        public virtual FreshPanelButton? Button => null;

        // On the element's laid-out area (UI units, two a UI pixel), on whole pixels.
        public void Place()
        {
            CalculatedStyle area = Element.GetDimensions();
            Vector2 origin = Origin();
            area.X -= origin.X;
            area.Y -= origin.Y;
            float x = MathF.Round(area.X / 2), y = MathF.Round(area.Y / 2);
            float width = MathF.Round((area.X + area.Width) / 2) - x, height = MathF.Round((area.Y + area.Height) / 2) - y;
            GraphicalUiElement visual = Visual;
            if (visual.X != x || visual.Y != y || visual.Width != width || visual.Height != height)
                (visual.X, visual.Y, visual.Width, visual.Height) = (x, y, width, height);
        }

        public virtual void Sync() => Place();
    }

    // Gum's clicks, hovers and focus on a control reach the element as the game's mouse events.
    private static void Forward(Gum.Forms.Controls.FrameworkElement control, InteractiveGue visual, UIElement element, Action<Action> onClick)
    {
        UIMouseEvent At(Vector2 position) => new(element, position);
        onClick(() =>
        {
            // At the element (UI units): Gum's mouse isn't in the game's UI space here, and A on a focus has no mouse.
            UIMouseEvent evt = At(element.GetDimensions().Center());
            element.LeftMouseDown(evt);
            element.LeftMouseUp(evt);
            element.LeftClick(evt);
        });
        visual.RollOn += (_, _) => element.MouseOver(At(new Vector2(Main.mouseX, Main.mouseY)));
        visual.RollOff += (_, _) => element.MouseOut(At(new Vector2(Main.mouseX, Main.mouseY)));
        control.GotFocus += (_, _) => element.MouseOver(At(element.GetDimensions().Center()));
        control.LostFocus += (_, _) => element.MouseOut(At(element.GetDimensions().Center()));
    }

    /// <summary>A control in the menus' panel button look, acting as the element.</summary>
    private class ButtonMirror : Mirror
    {
        private readonly FreshPanelButton _button = new();

        public ButtonMirror(UIElement element, string text) : base(element)
        {
            _button.Text = text;
            _button.Visual.WidthUnits = _button.Visual.HeightUnits = DimensionUnitType.Absolute;
            Forward(_button, _button.Visual, element, click => _button.Click += (_, _) => click());
        }

        public override GraphicalUiElement Visual => _button.Visual;
        public override FreshPanelButton Button => _button;
    }

    /// <summary>A control with no look of its own, over the game's art: a place to click and focus (its outline while focused).</summary>
    private sealed class HitMirror : Mirror
    {
        private readonly FreshIconButton _target = new();

        public HitMirror(UIElement element) : base(element) =>
            Forward(_target, _target.Visual, element, click => _target.Click += (_, _) => click());

        public override GraphicalUiElement Visual => _target.Visual;
    }

    /// <summary>A text panel (Back, Create…): its text, in the game's size.</summary>
    private sealed class TextPanelMirror : ButtonMirror
    {
        private static readonly Dictionary<Type, (PropertyInfo Text, PropertyInfo Large, PropertyInfo Scale)> Members = [];
        private readonly PropertyInfo _textProperty;

        public TextPanelMirror(UIPanel panel, bool listened) : base(panel, "")
        {
            Type type = panel.GetType();
            if (!Members.TryGetValue(type, out var members))
                members = Members[type] = (type.GetProperty("Text")!, type.GetProperty("IsLarge")!, type.GetProperty("TextScale")!);
            _textProperty = members.Text;
            if (!listened)
                Button.Visual.HasEvents = false;
            Button.FontSize = ((bool)members.Large.GetValue(panel)! ? DeathTextEm : MouseTextEm) / 2 * (float)members.Scale.GetValue(panel)!;
        }

        public override void Sync()
        {
            base.Sync();
            string text = (string)_textProperty.GetValue(Element)! ?? "";
            if (Button.Text != text)
                Button.Text = text;
        }
    }

    /// <summary>
    /// A text box (the game's internal <c>UITextBox</c>, the on-screen keyboard's): not a control, a panel in the
    /// panel buttons' look showing its text, what the IME is composing and the caret at the end (where the game keeps
    /// it), in the game's size, hidden while the game hides the box.
    /// </summary>
    private sealed class TextBoxMirror : Mirror
    {
        private readonly FreshPanel _panel = new() { BackgroundColor = FreshStyle.Button };
        private readonly TextRuntime _text = new()
        {
            XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center,
            YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            Color = Color.White,
            HasEvents = false,
        };
        private readonly UITextPanel<string> _box;
        private readonly FieldInfo _showInputTicker, _hideSelf, _cursor;

        public TextBoxMirror(UITextPanel<string> box) : base(box)
        {
            _box = box;
            _showInputTicker = box.GetType().GetField("ShowInputTicker")!;
            _hideSelf = box.GetType().GetField("HideSelf")!;
            _cursor = box.GetType().GetField("_cursor", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _text.FontSize = (box.IsLarge ? DeathTextEm : MouseTextEm) / 2 * box.TextScale;
            GumText.SetOutline(_text, 0.75f, Color.Black);
            _panel.Children.Add(_text);
        }

        public override GraphicalUiElement Visual => _panel;

        // shortcut: the caret doesn't blink (the game's does), so the centred text doesn't shift as it would.
        public override void Sync()
        {
            base.Sync();
            bool shown = !(bool)_hideSelf.GetValue(_box)!;
            if (_panel.Visible != shown)
                _panel.Visible = shown;
            string text = _box.Text ?? "";
            // As the game's box sets it each draw: what's typed goes at the end.
            _cursor.SetValue(_box, text.Length);
            if ((bool)_showInputTicker.GetValue(_box)!)
                text += ReLogic.OS.Platform.Get<ReLogic.Localization.IME.IImeService>().CompositionString + "|";
            if (_text.Text != text)
                _text.Text = text;
        }
    }

    /// <summary>An option button: its icon, chosen while it's the option picked.</summary>
    private sealed class OptionMirror : ButtonMirror
    {
        private readonly PropertyInfo _selected, _icon, _iconColor;
        private readonly FieldInfo _highlight, _frame, _overrides, _picked, _unpicked;
        private Texture2D? _shownIcon;
        private Color _shownColor;

        public OptionMirror(UIElement element) : base(element, "")
        {
            Type type = element.GetType();
            _selected = type.GetProperty("IsSelected")!;
            _icon = type.GetProperty("Icon")!;
            _iconColor = type.GetProperty("IconColor")!;
            _highlight = type.GetField("ShowHighlightWhenSelected")!;
            _frame = type.GetField("_iconFrame", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _overrides = type.GetField("_UseOverrideColors", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _picked = type.GetField("_overridePickedColor", BindingFlags.NonPublic | BindingFlags.Instance)!;
            _unpicked = type.GetField("_overrideUnpickedColor", BindingFlags.NonPublic | BindingFlags.Instance)!;
        }

        public override void Sync()
        {
            base.Sync();
            // Chosen while selected, where the game shows it: its highlight, or its own picked colour (the journey powers').
            bool chosen = (bool)_selected.GetValue(Element)! && ((bool)_highlight.GetValue(Element)! ||
                (bool)_overrides.GetValue(Element)! && !_picked.GetValue(Element)!.Equals(_unpicked.GetValue(Element)));
            Button.IsChosen = chosen;
            var icon = (Texture2D?)_icon.GetValue(Element);
            var color = (Color)_iconColor.GetValue(Element)!;
            if (icon != _shownIcon || color != _shownColor)
            {
                (_shownIcon, _shownColor) = (icon, color);
                Button.SetIcon(icon, (Rectangle?)_frame.GetValue(Element), color);
                // An icon on its own (no title in it) is centred.
                if (!Element.Children.OfType<UIText>().Any(t => t.Text.Length > 0))
                    Button.CentreIcon();
            }
        }
    }

    /// <summary>An image button (a category, copy, paste, random…): its icon in its colour, chosen while selected.</summary>
    private sealed class ImageButtonMirror : ButtonMirror
    {
        private static readonly FieldInfo Texture = typeof(UIColoredImageButton).GetField("_texture", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo IconColor = typeof(UIColoredImageButton).GetField("_color", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo Selected = typeof(UIColoredImageButton).GetField("_selected", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private Texture2D? _shownIcon;
        private Color _shownColor;

        public ImageButtonMirror(UIColoredImageButton element) : base(element, "") { }

        public override void Sync()
        {
            base.Sync();
            Button.IsChosen = (bool)Selected.GetValue(Element)!;
            Texture2D icon = ((ReLogic.Content.Asset<Texture2D>)Texture.GetValue(Element)!).Value;
            var color = (Color)IconColor.GetValue(Element)!;
            if (icon != _shownIcon || color != _shownColor)
            {
                (_shownIcon, _shownColor) = (icon, color);
                Button.SetIcon(icon, null, color);
                Button.CentreIcon();
            }
        }
    }

    /// <summary>An image button with a label on it: a <see cref="FreshPanelButton"/> with the label's text, in its size.</summary>
    private sealed class LabelledImageButtonMirror : ButtonMirror
    {
        private readonly UIText _label;

        public LabelledImageButtonMirror(UIImageButton element, UIText label) : base(element, "")
        {
            _label = label;
            Button.FontSize = MouseTextEm / 2 * (float)TextScale.GetValue(label)!;
        }

        public override void Sync()
        {
            base.Sync();
            if (Button.Text != _label.Text)
                Button.Text = _label.Text;
        }
    }

    /// <summary>A difficulty: chosen while it's the player's.</summary>
    private sealed class DifficultyMirror(UIDifficultyButton element) : ButtonMirror(element, "")
    {
        private static readonly FieldInfo Player = typeof(UIDifficultyButton).GetField("_player", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo Value = typeof(UIDifficultyButton).GetField("_difficulty", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override void Sync()
        {
            base.Sync();
            Button.IsChosen = ((Player)Player.GetValue(Element)!).difficulty == (byte)Value.GetValue(Element)!;
        }
    }

    /// <summary>A colour slider: a <see cref="FreshSlider"/> on the game's value, its track the game's colours.</summary>
    private sealed class SliderMirror : Mirror
    {
        private static readonly FieldInfo Get = typeof(UIColoredSlider).GetField("_getStatusTextAct", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo Set = typeof(UIColoredSlider).GetField("_slideKeyboardAction", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo Blip = typeof(UIColoredSlider).GetField("_blipFunc", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private readonly FreshSlider _slider = new() { Minimum = 0, Maximum = 1 };
        private readonly Func<float> _get;
        private bool _syncing;

        public SliderMirror(UIColoredSlider element) : base(element)
        {
            _get = (Func<float>)Get.GetValue(element)!;
            var set = (Action<float>)Set.GetValue(element)!;
            var blip = (Func<float, Color>)Blip.GetValue(element)!;
            _slider.Visual.WidthUnits = DimensionUnitType.Absolute;
            _slider.Ramp = blip;
            _slider.ValueChanged += (_, _) =>
            {
                if (!_syncing)
                    set((float)_slider.Value);
            };
        }

        public override GraphicalUiElement Visual => _slider.Visual;

        public override void Sync()
        {
            base.Sync();
            float value = _get();
            if (MathF.Abs((float)_slider.Value - value) > 1e-4f)
            {
                _syncing = true;
                _slider.Value = value;
                _syncing = false;
            }
            // The other bars' colours move this one's track.
            _slider.UpdateRamp();
        }
    }

    /// <summary>A list: a <see cref="FreshScrollViewer"/> holding its items, as tall as their content.</summary>
    private sealed class ListMirror : Mirror
    {
        private readonly FreshScrollViewer _viewer = new();
        private readonly UIList _list;

        public ListMirror(UIList list) : base(list)
        {
            _list = list;
            _viewer.Visual.WidthUnits = _viewer.Visual.HeightUnits = DimensionUnitType.Absolute;
            _viewer.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.Regular;
            _viewer.InnerPanel.HeightUnits = DimensionUnitType.Absolute;
        }

        /// <summary>The container its items' components go in.</summary>
        public GraphicalUiElement Items => _viewer.InnerPanel;

        public override GraphicalUiElement Visual => _viewer.Visual;

        public override void Sync()
        {
            base.Sync();
            float height = MathF.Ceiling(_list.GetTotalHeight() / 2);
            if (_viewer.InnerPanel.Height != height)
                _viewer.InnerPanel.Height = height;
        }
    }

    private sealed class PanelMirror : Mirror
    {
        private readonly FreshPanel _panel = new();
        private readonly Func<Color> _color;
        private readonly Func<Color>? _border;

        public PanelMirror(UIElement element, Func<Color> color, Func<Color>? border = null, bool takesMouse = false) : base(element)
        {
            _color = color;
            _border = border;
            // Where asked (PanelsTakeMouse), unless the game's ignores the mouse (a border laid over a page).
            _panel.HasEvents = takesMouse && !element.IgnoresMouseInteraction;
        }

        public override GraphicalUiElement Visual => _panel;

        public override void Sync()
        {
            base.Sync();
            Color color = _color();
            if (_panel.BackgroundColor != color)
                _panel.BackgroundColor = color;
            // The game's border (a panel's own colour), else black at the fill's alpha.
            Color border = _border?.Invoke() ?? Color.Black * (color.A / 255f);
            if (_panel.BorderColor != border)
                _panel.BorderColor = border;
        }
    }

    private sealed class SeparatorMirror(UIElement element) : Mirror(element)
    {
        private readonly FreshSeparator _separator = new() { WidthUnits = DimensionUnitType.Absolute };

        public override GraphicalUiElement Visual => _separator;
    }

    /// <summary>Text in the game's font size (Andy Bold either way) and colour, placed on the game's baseline.</summary>
    private sealed class TextMirror : Mirror
    {
        private readonly UIText _source;
        private readonly TextRuntime _text = new() { HasEvents = false };

        public TextMirror(UIText text) : base(text)
        {
            _source = text;
            GumText.SetOutline(_text, 0.75f, Color.Black);
        }

        public override GraphicalUiElement Visual => _text;

        public override void Sync()
        {
            CalculatedStyle inner = _source.GetInnerDimensions();
            Vector2 origin = Origin();
            inner.X -= origin.X;
            inner.Y -= origin.Y;
            float scale = (float)TextScale.GetValue(_source)!;
            bool large = (bool)TextLarge.GetValue(_source)!;
            float size = (large ? DeathTextEm : MouseTextEm) / 2 * scale, baseline = (large ? DeathTextBaseline : MouseTextBaseline) * scale;
            if (_text.FontSize != size)
                _text.FontSize = size;
            string value = _source.Text;
            if (_text.Text != value)
                _text.Text = value;
            if (_text.Color != _source.TextColor)
                _text.Color = _source.TextColor;
            var align = _source.TextOriginX switch
            {
                <= 0.25f => RenderingLibrary.Graphics.HorizontalAlignment.Left,
                >= 0.75f => RenderingLibrary.Graphics.HorizontalAlignment.Right,
                _ => RenderingLibrary.Graphics.HorizontalAlignment.Center,
            };
            if (_text.HorizontalAlignment != align)
                _text.HorizontalAlignment = align;
            float x = MathF.Round(inner.X / 2), y = MathF.Round((inner.Y + baseline) / 2 - size * GumText.Baseline), width = MathF.Round(inner.Width / 2);
            if (!_source.IsWrapped)
            {
                // A line the game doesn't wrap isn't wrapped here (Gum's letters can be a little wider than the game's
                // measure): as wide as it is, anchored at the side of the game's area it's aligned to.
                x += align switch
                {
                    RenderingLibrary.Graphics.HorizontalAlignment.Right => width,
                    RenderingLibrary.Graphics.HorizontalAlignment.Center => MathF.Round(width / 2),
                    _ => 0,
                };
                width = 0;
            }
            if (_text.X != x || _text.Y != y || _text.Width != width)
            {
                (_text.X, _text.Y, _text.Width) = (x, y, width);
                _text.WidthUnits = _source.IsWrapped ? DimensionUnitType.Absolute : DimensionUnitType.RelativeToChildren;
                _text.XOrigin = _source.IsWrapped ? RenderingLibrary.Graphics.HorizontalAlignment.Left : align;
                _text.HeightUnits = DimensionUnitType.RelativeToChildren;
                _text.Height = 0;
            }
        }
    }

    /// <summary>
    /// The game's own drawing of a leaf (a preview, a portrait), in Gum's order; in a list, moved with the list's
    /// scroll (the game draws it where it laid it out) and clipped to the list's view.
    /// </summary>
    private sealed class GameMirror : Mirror
    {
        private static readonly RasterizerState Clipped = new() { CullMode = CullMode.CullCounterClockwiseFace, ScissorTestEnable = true };
        private readonly FreshGameView _view;
        private readonly ListMirror? _list;
        private readonly GameUiMirror _owner;

        public GameMirror(UIElement element, ListMirror? list, GameUiMirror owner) : base(element)
        {
            _list = list;
            _owner = owner;
            _view = new FreshGameView { Draw = DrawAtCursor };
        }

        public override GraphicalUiElement Visual => _view;

        // What acts on the game's mouse as it draws (an item slot, an item grid) finds it at the gamepad's cursor, as in the game's own UI draw.
        private void DrawAtCursor(SpriteBatch batch, Rectangle area)
        {
            if (!_owner.FollowsGameCursor || GamepadCursor() is not { } cursor)
            {
                Draw(batch, area);
                return;
            }
            (int mouseX, int mouseY) = (Main.mouseX, Main.mouseY);
            (Main.mouseX, Main.mouseY) = ((int)cursor.X, (int)cursor.Y);
            try
            {
                Draw(batch, area);
            }
            finally
            {
                (Main.mouseX, Main.mouseY) = (mouseX, mouseY);
            }
        }

        private void Draw(SpriteBatch batch, Rectangle area)
        {
            if (_list == null)
            {
                Element.Draw(batch);
                return;
            }
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Matrix transform = GumHost.BatchTransform(batch);
            Vector2 offset = area.Location.ToVector2() - Element.GetDimensions().Position();
            GraphicalUiElement view = _list.Visual;
            float scale = UiPixels.Scale;
            Rectangle clip = Rectangle.Intersect(device.Viewport.Bounds, new Rectangle((int)(view.AbsoluteLeft * scale), (int)(view.AbsoluteTop * scale),
                (int)(view.GetAbsoluteWidth() * scale), (int)(view.GetAbsoluteHeight() * scale)));
            if (clip.Width <= 0 || clip.Height <= 0)
                return;
            Rectangle scissor = device.ScissorRectangle;
            batch.End();
            device.ScissorRectangle = clip;
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, FreshUI.Hooks.Sharp.Snapped, DepthStencilState.None, Clipped, null,
                Matrix.CreateTranslation(offset.X, offset.Y, 0) * transform);
            try
            {
                Element.Draw(batch);
            }
            finally
            {
                batch.End();
                device.ScissorRectangle = scissor;
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, FreshUI.Hooks.Sharp.Snapped, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, transform);
            }
        }
    }
}
