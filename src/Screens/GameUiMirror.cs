using System.Reflection;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
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
    private int _builtCount;
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
        int count = Count(Source);
        if (Source != _built || count != _builtCount)
        {
            _built = Source;
            _builtCount = count;
            Build();
        }
        foreach (Mirror mirror in _mirrors)
            mirror.Sync();
    }

    private static int Count(UIElement element)
    {
        int count = 1;
        foreach (UIElement child in element.Children)
            count += Count(child);
        return count;
    }

    private void Build()
    {
        _zone.Children.Clear();
        _mirrors.Clear();
        foreach (UIElement child in Source!.Children)
            Add(child);
    }

    private static bool Listened(UIElement element) => MouseDownEvent.GetValue(element) != null || ClickEvent.GetValue(element) != null;

    // An element and what's in it.
    private void Add(UIElement element)
    {
        Type type = element.GetType();
        bool listened = Listened(element);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GroupOptionButton<>))
            Keep(new OptionMirror(element));
        else if (element is UITextPanel<string> or UITextPanel<Terraria.Localization.LocalizedText> || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UITextPanel<>))
            Keep(new TextPanelMirror((UIPanel)element, listened));
        else if (element is UICharacterNameButton || listened && element is UIPanel)
            Keep(new ButtonMirror(element, ""));
        else if (element is UIPanel panel)
            Keep(new PanelMirror(element, () => panel.BackgroundColor));
        else if (element is UISlicedImage image)
            Keep(new PanelMirror(element, () => image.Color));
        else if (element is UIHorizontalSeparator)
            Keep(new SeparatorMirror(element));
        else if (element is UIText text)
            Keep(new TextMirror(text));
        else if (!element.Children.Any() && element.GetType() != typeof(UIElement))
        {
            // The game's own art (a preview, a portrait…); a control too where the game listens to it.
            Keep(new GameMirror(element));
            if (listened)
                Keep(new HitMirror(element));
            return;
        }
        else if (listened)
            Keep(new HitMirror(element));
        foreach (UIElement child in element.Children)
            Add(child);
    }

    private void Keep(Mirror mirror)
    {
        _mirrors.Add(mirror);
        _zone.Children.Add(mirror.Visual);
        mirror.Place();
    }

    /// <summary>A component standing for a game element.</summary>
    private abstract class Mirror(UIElement element)
    {
        public readonly UIElement Element = element;
        public abstract GraphicalUiElement Visual { get; }
        public virtual FreshPanelButton? Button => null;

        // On the element's laid-out area (UI units, two a UI pixel), on whole pixels.
        public void Place()
        {
            CalculatedStyle area = Element.GetDimensions();
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
            UIMouseEvent evt = At(new Vector2(Main.mouseX, Main.mouseY));
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

    /// <summary>An option button: its icon, chosen while it's the option picked.</summary>
    private sealed class OptionMirror : ButtonMirror
    {
        private readonly PropertyInfo _selected, _icon, _iconColor;
        private readonly FieldInfo _highlight, _frame;
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
        }

        public override void Sync()
        {
            base.Sync();
            bool chosen = (bool)_selected.GetValue(Element)! && (bool)_highlight.GetValue(Element)!;
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

    private sealed class PanelMirror : Mirror
    {
        private readonly FreshPanel _panel = new();
        private readonly Func<Color> _color;

        public PanelMirror(UIElement element, Func<Color> color) : base(element) => _color = color;

        public override GraphicalUiElement Visual => _panel;

        public override void Sync()
        {
            base.Sync();
            Color color = _color();
            if (_panel.BackgroundColor != color)
            {
                _panel.BackgroundColor = color;
                _panel.BorderColor = Color.Black * (color.A / 255f);
            }
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
            if (_text.X != x || _text.Y != y || _text.Width != width)
            {
                (_text.X, _text.Y, _text.Width) = (x, y, width);
                _text.WidthUnits = DimensionUnitType.Absolute;
                _text.HeightUnits = DimensionUnitType.RelativeToChildren;
                _text.Height = 0;
            }
        }
    }

    /// <summary>The game's own drawing of a leaf (a preview, a portrait), in Gum's order.</summary>
    private sealed class GameMirror : Mirror
    {
        private readonly FreshGameView _view;

        public GameMirror(UIElement element) : base(element) => _view = new FreshGameView { Draw = (batch, _) => Element.Draw(batch) };

        public override GraphicalUiElement Visual => _view;
    }
}
