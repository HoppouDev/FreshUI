using System.Reflection;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The emotes (the game's <see cref="UIEmotesMenu"/>, a fancy UI from the inventory's emote button) in the menus'
/// window: the game's groups (general, rock-paper-scissors, items, biomes and events, town NPCs, critters, bosses;
/// the critters and bosses as the world has met them, from the game's own lists) under muted headers, each emote a
/// button in its bubble, animated as the game's, its border gold while hovered or focused, its "/name" as its
/// tooltip; a click (or A) emotes and closes the menu, as the game's. Back closes it.
/// </summary>
internal sealed class EmotesScreen : ContainerRuntime, IGumScreenUpdate
{
    private const float Padding = 6, ActionHeight = 25, Gap = 4, WindowWidth = 300, EmoteGap = 2;

    private static readonly Func<bool> Route = GumRoutes.State<UIEmotesMenu>();
    private static bool _wasShown;

    // The game's groups: their titles and lists (private, as the world has met the critters and bosses).
    private static readonly (string Title, string Method)[] Groups =
    [
        ("UI.EmoteCategoryGeneral", "GetEmotesGeneral"), ("UI.EmoteCategoryRPS", "GetEmotesRPS"),
        ("UI.EmoteCategoryItems", "GetEmotesItems"), ("UI.EmoteCategoryBiomesAndEvents", "GetEmotesBiomesAndEvents"),
        ("UI.EmoteCategoryTownNPCs", "GetEmotesTownNPCs"), ("UI.EmoteCategoryCritters", "GetEmotesCritters"),
        ("UI.EmoteCategoryBosses", "GetEmotesBosses"),
    ];

    /// <summary>Whether the game's emote menu shows (building its emotes as it opens).</summary>
    public static bool Shows()
    {
        bool shown = Route();
        if (shown && !_wasShown && Main.InGameUI.CurrentState is UIEmotesMenu menu)
            Instance?.Open(menu);
        _wasShown = shown;
        return shown;
    }

    public static EmotesScreen? Instance { get; private set; }

    private readonly RectangleRuntime _backdrop = FreshStyle.Backdrop(FreshStyle.Dim);
    // The game's menu has no title: FreshUI's own English.
    private readonly FreshWindow _window = new("Emotes");
    private readonly FreshPanel _panel = new();
    private readonly FreshScrollViewer _list = new() { WheelStep = 20, IsFocusable = false };
    private readonly List<EmoteButton> _emotes = [];
    private int _frameCounter;

    public EmotesScreen()
    {
        Instance = this;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = true;
        Children.Add(_backdrop);
        InteractiveGue zone = GumGamepad.SpatialZone();
        Children.Add(zone);
        zone.Children.Add(_window);
        _window.Add(_panel);
        _panel.WidthUnits = _panel.HeightUnits = DimensionUnitType.RelativeToParent;
        _panel.Width = _panel.Height = 0;
        _list.Visual.X = _list.Visual.Y = Padding;
        _list.Visual.WidthUnits = _list.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        _list.Visual.Width = -2 * Padding;
        _list.Visual.Height = -(2 * Padding + ActionHeight + Gap);
        _list.InnerPanel.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _list.InnerPanel.StackSpacing = Gap;
        _panel.Children.Add(_list.Visual);

        var back = new FreshPanelButton { Text = Language.GetTextValue("UI.Back") };
        back.Visual.X = Padding;
        back.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        back.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        back.Visual.Y = -Padding;
        back.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        back.Visual.Width = -2 * Padding;
        back.Visual.HeightUnits = DimensionUnitType.Absolute;
        back.Visual.Height = ActionHeight;
        back.Click += (_, _) =>
        {
            Main.menuMode = 0;
            IngameFancyUI.Close();
        };
        _panel.Children.Add(back.Visual);
    }

    /// <summary>The gamepad's starting point: the first emote.</summary>
    public FrameworkElement? First => _emotes.Count > 0 ? _emotes[0] : null;

    // As the game's menu builds itself each opening: each group with any emotes, its title and a grid of them.
    private void Open(UIEmotesMenu menu)
    {
        _list.InnerPanel.Children.Clear();
        _emotes.Clear();
        foreach ((string title, string method) in Groups)
        {
            var emotes = (List<int>?)typeof(UIEmotesMenu).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(menu, null);
            if (emotes == null || emotes.Count == 0)
                continue;
            var header = new TextRuntime
            {
                WidthUnits = DimensionUnitType.RelativeToChildren,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                X = 2,
                FontSize = 10.5f,
                Text = Language.GetTextValue(title),
                Color = FreshStyle.MutedText,
                HasEvents = false,
            };
            GumText.SetOutline(header, 0.75f, Color.Black);
            _list.InnerPanel.Children.Add(header);
            var grid = new ContainerRuntime
            {
                WidthUnits = DimensionUnitType.RelativeToParent,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                ChildrenLayout = ChildrenLayout.LeftToRightStack,
                WrapsChildren = true,
                StackSpacing = EmoteGap,
                HasEvents = false,
            };
            foreach (int emote in emotes)
            {
                var button = new EmoteButton(emote);
                _emotes.Add(button);
                grid.Children.Add(button.Visual);
            }
            _list.InnerPanel.Children.Add(grid);
        }
        _list.VerticalScrollBarValue = 0;
        _frameCounter = 0;
    }

    public void UpdateScreen()
    {
        bool inGame = !Main.gameMenu;
        if (_backdrop.Visible != inGame)
            _backdrop.Visible = inGame;
        _window.PlaceAsMenu(inGame, WindowWidth);
        // The emotes' two frames, ten updates each, as the game's buttons.
        if (++_frameCounter >= 20)
            _frameCounter = 0;
        if (_frameCounter is 0 or 10)
        {
            foreach (EmoteButton emote in _emotes)
                emote.SetFrame(_frameCounter / 10);
        }
    }

    /// <summary>An emote's button: its bubble, the emote, and the bubble's border, gold while lit (hovered, focused).</summary>
    private sealed class EmoteButton : Button
    {
        private readonly int _emote;
        private readonly SpriteRuntime _face, _border;

        public EmoteButton(int emote)
        {
            _emote = emote;
            Texture2D sheet = GumTextures.Load("Images/Extra_48");
            Rectangle frame = sheet.Frame(8, EmoteBubble.EMOTE_SHEET_VERTICAL_FRAMES);
            Texture2D border = GumTextures.Load("Images/UI/EmoteBubbleBorder");
            var visual = (ButtonVisual)Visual;
            visual.Background.Visible = false;
            visual.FocusedIndicator.Visible = false;
            visual.TextInstance.Visible = false;
            visual.WidthUnits = visual.HeightUnits = DimensionUnitType.Absolute;
            (visual.Width, visual.Height) = (frame.Width / 2f, frame.Height / 2f);
            // Its name for automation (the text itself isn't shown).
            Text = EmoteName;
            visual.Children.Add(Sprite(sheet, new Rectangle(frame.Width, 0, frame.Width, frame.Height), 0, 0));
            _face = Sprite(sheet, frame, 0, 0);
            visual.Children.Add(_face);
            // The border's top left is two texels up and left of the bubble's, as the game's: a UI pixel.
            _border = Sprite(border, border.Bounds, -1, -1);
            _border.Color = Color.Black;
            visual.Children.Add(_border);
            SetFrame(0);
            foreach (StateSave state in new[] { visual.States.Enabled, visual.States.Focused, visual.States.Highlighted,
                visual.States.HighlightedFocused, visual.States.Pushed, visual.States.Disabled, visual.States.DisabledFocused })
            {
                bool lit = state != visual.States.Enabled && state != visual.States.Disabled && state != visual.States.DisabledFocused;
                state.Clear();
                state.Apply = () =>
                {
                    visual.FocusedIndicator.Visible = false;
                    visual.TextInstance.Visible = false;
                    Color color = lit ? Main.OurFavoriteColor : Color.Black;
                    if (lit && _border.Color != color)
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    _border.Color = color;
                };
            }
            UpdateState();
            Click += (_, _) =>
            {
                EmoteBubble.MakeLocalPlayerEmote(_emote);
                IngameFancyUI.Close();
            };
            FreshTooltip.Attach(this, () => EmoteName);
        }

        private string EmoteName => "/" + Language.GetTextValue("EmojiName." + EmoteID.Search.GetName(_emote));

        /// <summary>Shows the emote's first or second frame.</summary>
        public void SetFrame(int frame)
        {
            Texture2D sheet = (Texture2D)_face.Texture;
            Rectangle source = sheet.Frame(8, EmoteBubble.EMOTE_SHEET_VERTICAL_FRAMES, _emote % 4 * 2 + frame, _emote / 4 + 1);
            (_face.TextureLeft, _face.TextureTop) = (source.X, source.Y);
        }

        // The game's art at its size: 2×2 texels a UI pixel.
        private static SpriteRuntime Sprite(Texture2D texture, Rectangle source, float x, float y) => new()
        {
            Texture = texture,
            TextureAddress = TextureAddress.Custom,
            TextureLeft = source.X,
            TextureTop = source.Y,
            TextureWidth = source.Width,
            TextureHeight = source.Height,
            X = x,
            Y = y,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = source.Width / 2f,
            Height = source.Height / 2f,
        };
    }
}
