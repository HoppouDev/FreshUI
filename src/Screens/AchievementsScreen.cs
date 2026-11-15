using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The achievements (the game's <see cref="UIAchievementsMenu"/>, on the title menu and as a fancy UI in game) in the
/// menus' window: the four categories as toggles that filter the list (all on as each opening starts, as the game's),
/// a row per achievement in the game's order (its icon in its border, locked or not; its name in gold once completed
/// with its category's icon; its tracker's count and a progress bar while locked; its description), lit while
/// hovered, and Back. The game's <c>GotoAchievement</c> (an "achievement unlocked" notification's click) scrolls to
/// its row (<see cref="Goto"/>).
/// </summary>
internal sealed class AchievementsScreen : ContainerRuntime, IGumScreenUpdate
{
    private const float Padding = 6, ToggleSize = 16, ActionHeight = 25, Gap = 4;
    // A row, in UI pixels: the icon (32, the game's 64 texels at half a pixel each) in its border (36) at the top left,
    // the same gap (4) around the border; the name and description beside it, centred on the icon.
    private const float RowHeight = 44, Border = 36, TextX = 4 + Border + 6, ProgressWidth = 50;

    private static readonly Func<bool> Route = GumRoutes.State<UIAchievementsMenu>();
    private static bool _wasShown;
    private static Achievement? _goto;

    /// <summary>Whether the game's achievements show (noting when they open).</summary>
    public static bool Shows()
    {
        bool shown = Route();
        if (shown && !_wasShown)
            Instance?.Open();
        _wasShown = shown;
        return shown;
    }

    /// <summary>The game's <c>GotoAchievement</c>: its row is scrolled to the top of the list.</summary>
    public static void Goto(Achievement achievement) => _goto = achievement;

    public static AchievementsScreen? Instance { get; private set; }

    private static readonly string[] CategoryNames = ["Achievements.SlayerCategory", "Achievements.CollectorCategory", "Achievements.ExplorerCategory", "Achievements.ChallengerCategory"];

    private readonly RectangleRuntime _backdrop = FreshStyle.Backdrop(FreshStyle.Dim);
    private readonly FreshWindow _window = new(Language.GetTextValue("UI.Achievements"));
    private readonly FreshPanel _panel = new();
    private readonly FreshScrollViewer _list = new() { WheelStep = RowHeight + Gap };
    private readonly FreshPanelButton _back;
    private readonly List<FreshIconButton> _categories = [];
    private readonly List<Row> _rows = [];

    public AchievementsScreen()
    {
        Instance = this;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        Children.Add(_backdrop);
        InteractiveGue zone = GumGamepad.SpatialZone();
        Children.Add(zone);
        zone.Children.Add(_window);
        _window.Add(_panel);
        _panel.WidthUnits = _panel.HeightUnits = DimensionUnitType.RelativeToParent;
        _panel.Width = _panel.Height = 0;

        Texture2D categories = GumTextures.Load("Images/UI/Achievement_Categories");
        for (int i = 0; i < 4; i++)
        {
            int category = i;
            var toggle = new FreshIconButton
            {
                Look = new FreshIconButtonLook(categories, new Rectangle(34 * i, 34, 32, 32)),
                CheckedLook = new FreshIconButtonLook(categories, new Rectangle(34 * i, 0, 32, 32)),
                TexelSize = 0.5f,
                IsToggle = true,
                IsChecked = true,
                HasBacking = true,
            };
            toggle.Visual.X = Padding + i * (ToggleSize + Gap);
            toggle.Visual.Y = Padding;
            toggle.Visual.Name = Language.GetTextValue(CategoryNames[category]);
            toggle.Click += (_, _) =>
            {
                SoundEngine.PlaySound(12);
                Filter();
            };
            FreshTooltip.Attach(toggle, () => Language.GetTextValue(CategoryNames[category]));
            _categories.Add(toggle);
            _panel.Children.Add(toggle.Visual);
        }

        _list.Visual.X = Padding;
        _list.Visual.Y = Padding + ToggleSize + Gap;
        _list.Visual.WidthUnits = _list.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        _list.Visual.Width = -2 * Padding;
        _list.Visual.Height = -(2 * Padding + ToggleSize + Gap + ActionHeight + Gap);
        _list.InnerPanel.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _list.InnerPanel.StackSpacing = Gap;
        _panel.Children.Add(_list.Visual);

        _back = new FreshPanelButton { Text = Language.GetTextValue("UI.Back") };
        _back.Visual.X = Padding;
        _back.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        _back.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        _back.Visual.Y = -Padding;
        _back.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        _back.Visual.Width = -2 * Padding;
        _back.Visual.HeightUnits = DimensionUnitType.Absolute;
        _back.Visual.Height = ActionHeight;
        _back.Click += (_, _) => Back();
        _panel.Children.Add(_back.Visual);
    }

    /// <summary>The gamepad's starting point: the first category.</summary>
    public FrameworkElement First => _categories[0];

    // Each opening, as the game's: every category on, the rows as the achievements stand now.
    private void Open()
    {
        foreach (FreshIconButton toggle in _categories)
            toggle.IsChecked = true;
        _rows.Clear();
        foreach (Achievement achievement in Main.Achievements.CreateAchievementsList())
            _rows.Add(new Row(achievement));
        Filter();
    }

    private void Filter()
    {
        _list.InnerPanel.Children.Clear();
        foreach (Row row in _rows)
        {
            if (_categories[(int)row.Achievement.Category].IsChecked)
                _list.InnerPanel.Children.Add(row.Panel);
        }
        _list.VerticalScrollBarValue = 0;
    }

    private static void Back()
    {
        Main.menuMode = 0;
        IngameFancyUI.Close();
    }

    public void UpdateScreen()
    {
        bool inGame = !Main.gameMenu;
        // Modal in game (the game takes no clicks while it shows), over the game dimmed.
        if (_backdrop.Visible != inGame)
            _backdrop.Visible = HasEvents = inGame;
        _window.PlaceAsMenu(inGame);
        if (_goto != null)
        {
            Row? row = _rows.Find(r => r.Achievement == _goto);
            if (row?.Panel.Parent != null)
                _list.VerticalScrollBarValue = row.Panel.AbsoluteTop - _list.InnerPanel.AbsoluteTop;
            _goto = null;
        }
        Gum.Wireframe.ICursor cursor = FrameworkElement.MainCursor;
        float x = cursor.XRespectingGumZoomAndBounds(), y = cursor.YRespectingGumZoomAndBounds();
        bool inList = Contains(_list.Visual, x, y);
        foreach (Row row in _rows)
        {
            if (row.Panel.Parent != null)
                row.Update(inList && Contains(row.Panel, x, y));
        }
        // On the title menu, Esc and B (the inventory key) go back; in game the game closes the fancy UI.
        if (Main.gameMenu)
        {
            bool escape = !PlayerInput.UsingGamepad && Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape);
            bool b = UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock;
            if (b)
                UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            if (escape || b)
                Back();
        }
    }

    private static bool Contains(GraphicalUiElement element, float x, float y) =>
        x >= element.AbsoluteLeft && x < element.AbsoluteLeft + element.AbsoluteWidth && y >= element.AbsoluteTop && y < element.AbsoluteTop + element.AbsoluteHeight;

    /// <summary>An achievement's row.</summary>
    private sealed class Row
    {
        public readonly Achievement Achievement;
        public readonly FreshPanel Panel = new() { WidthUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = RowHeight };
        private readonly SpriteRuntime _icon, _category;
        private readonly TextRuntime _name, _description, _count;
        private readonly FreshProgressBar _progress = new() { Width = ProgressWidth };
        private readonly Rectangle _unlocked;
        private bool? _lit;

        public Row(Achievement achievement)
        {
            Achievement = achievement;
            Panel.Name = achievement.FriendlyName.Value;
            int index = Main.Achievements.GetIconIndex(achievement.Name);
            _unlocked = new Rectangle(index % 8 * 66, index / 8 * 66, 64, 64);
            _icon = Sprite("Images/UI/Achievements", 6, 6, 32);
            (_icon.TextureLeft, _icon.TextureTop, _icon.TextureWidth, _icon.TextureHeight) = (_unlocked.X, _unlocked.Y, 64, 64);
            SpriteRuntime frame = Sprite("Images/UI/Achievement_Borders", 4, 4, Border);
            frame.TextureAddress = TextureAddress.EntireTexture;
            _category = Sprite("Images/UI/Achievement_Categories", TextX, 11, 8);
            _category.TextureWidth = _category.TextureHeight = 32;
            _category.TextureLeft = 34 * (int)achievement.Category;
            Panel.Children.Add(_icon);
            Panel.Children.Add(frame);
            Panel.Children.Add(_category);
            _name = Text(achievement.FriendlyName.Value, 9.5f);
            (_name.X, _name.Y) = (TextX + 11, 8);
            Panel.Children.Add(_name);
            _description = Text(achievement.Description.Value, 8);
            (_description.X, _description.Y) = (TextX, 21);
            _description.WidthUnits = DimensionUnitType.RelativeToParent;
            _description.Width = -(TextX + Padding);
            Panel.Children.Add(_description);
            // The tracker's count and bar at the name line's right.
            _progress.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            _progress.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
            (_progress.X, _progress.Y) = (-Padding, 10);
            Panel.Children.Add(_progress);
            _count = Text("", 8);
            _count.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            _count.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
            (_count.X, _count.Y) = (-(Padding + ProgressWidth + 4), 8);
            Panel.Children.Add(_count);
        }

        private static SpriteRuntime Sprite(string texture, float x, float y, float size) => new()
        {
            Texture = GumTextures.Load(texture),
            TextureAddress = TextureAddress.Custom,
            X = x,
            Y = y,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = size,
            Height = size,
        };

        private static TextRuntime Text(string text, float size)
        {
            var runtime = new TextRuntime
            {
                WidthUnits = DimensionUnitType.RelativeToChildren,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                FontSize = size,
                Text = text,
                HasEvents = false,
            };
            GumText.SetOutline(runtime, 0.75f, Color.Black);
            return runtime;
        }

        // The game's colours: gold once completed (silver before), lighter while hovered; the row lit with a gold border.
        public void Update(bool hovered)
        {
            bool locked = !Achievement.IsCompleted;
            Rectangle frame = locked ? new Rectangle(_unlocked.X + 528, _unlocked.Y, 64, 64) : _unlocked;
            if (_icon.TextureLeft != frame.X || _icon.TextureTop != frame.Y)
                (_icon.TextureLeft, _icon.TextureTop, _icon.TextureWidth, _icon.TextureHeight) = (frame.X, frame.Y, 64, 64);
            (decimal value, decimal max) = Tracker();
            bool tracked = locked && (value != 0 || max != 0);
            _progress.Visible = _count.Visible = tracked;
            if (tracked)
            {
                _progress.Value = max > 0 ? (float)(value / max) : 0;
                string count = (int)value + "/" + (int)max;
                if (_count.Text != count)
                    _count.Text = count;
            }
            if (_lit == hovered && !tracked)
                return;
            _lit = hovered;
            Color name = Color.Lerp(locked ? Color.Silver : Color.Gold, Color.White, hovered ? 0.5f : 0);
            _name.Color = _count.Color = name;
            _description.Color = hovered ? Color.White : locked ? Color.DarkGray : Color.Silver;
            _category.Color = hovered ? Color.White : Color.Silver;
            Panel.BorderColor = hovered ? FreshStyle.Gold : Color.Black;
            Panel.BackgroundColor = hovered ? FreshStyle.Button : FreshStyle.Panel;
        }

        private (decimal Value, decimal Max) Tracker()
        {
            if (!Achievement.HasTracker)
                return (0, 0);
            return Achievement.GetTracker() switch
            {
                AchievementTracker<int> tracker => (tracker.Value, tracker.MaxValue),
                AchievementTracker<float> tracker => ((decimal)tracker.Value, (decimal)tracker.MaxValue),
                _ => (0, 0),
            };
        }
    }
}
