using System.Linq.Expressions;
using System.Reflection;
using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using ReLogic.Localization.IME;
using ReLogic.OS;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The NPC chat and sign box (Aether's <see cref="UINpcChat"/>): a <see cref="FreshPanel"/> at the top of the
/// screen with the NPC's (or sign's) text, the NPC's portrait and the corner item (an item the NPC mentions, or
/// the housing button), and its interactions (Shop, Happiness, Quest…) as <see cref="FreshValueButton"/>s along
/// its bottom, four to a line, each with its price and notification if it has one. The text and its
/// preparation stay the game's (<c>NPCChatPanel</c>: wrapped, with chat tags; a sign being edited with its
/// caret and IME), drawn by the game in a <see cref="FreshGameView"/> with the portrait, corner item and prices.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> NpcChatAsked = GumRoutes.GameScreen<UINpcChat>();
    private const float ChatWidth = 250, ChatTop = 50;
    private static readonly Color ChatButtonColor = new(255, 231, 128);

    private FreshPanel? _chatBox;
    private FreshGameView? _chatView;
    private readonly List<FreshValueButton> _chatButtons = [];
    private readonly UINpcChatCornerItem _chatCorner = new();
    private readonly ChatTextCache _chatLines = new();
    private NPCChatPanel? _chatPanel;
    private int _chatPricesShown;
    // Each button's price this frame (0: none), and where it's drawn, in UI units.
    private readonly List<(int Price, Vector2 At, bool Exclamation, Vector2 ExclamationAt)> _chatExtras = [];

    private void PresentNpcChat()
    {
        bool shown = IsReady && NpcChatAsked() && ChatPanel.GetValue(UINpcChat.Instance) is NPCChatPanel;
        if (_chatBox == null)
        {
            if (!shown)
                return;
            CreateNpcChat();
        }
        if (_chatBox!.Visible != shown)
            _chatBox.Visible = shown;
        if (!shown)
            return;
        NPCChatPanel panel = _chatPanel = (NPCChatPanel)ChatPanel.GetValue(UINpcChat.Instance)!;
        PrepareText(panel);
        PrepareInteractions(panel);
        PrepareVirtualKeyboard(panel);
        TextDisplayCache cache = TextCache(panel);
        int lines = NeededLines(panel);
        float x = MathF.Round((Gum.Wireframe.GraphicalUiElement.CanvasWidth - ChatWidth) / 2);
        float height = MathF.Ceiling((30 + 30 * cache.AmountOfLines + 30 * lines + Math.Max(0, 2 * (lines - 1))) / 2f);
        if (_chatBox.X != x || _chatBox.Height != height)
        {
            _chatBox.X = x;
            _chatBox.Height = height;
        }
        var area = new Rectangle((int)(x * 2), (int)(ChatTop * 2), (int)(ChatWidth * 2), (int)(height * 2));
        if (!PlayerInput.IgnoreMouseInterface && area.Contains(Main.mouseX, Main.mouseY))
            Main.LocalPlayer.mouseInterface = true;
        _chatCorner.BottomRight = new Vector2(area.Right, area.Bottom);

        // The interactions along the bottom, four to a line; one with a price leaves room for it.
        List<NPCInteraction> interactions = Interactions(panel);
        UILinkPointNavigator.Shortcuts.NPCCHAT_ButtonsNew = true;
        UILinkPointNavigator.Shortcuts.NPCCHAT_ButtonsCount = interactions.Count;
        while (_chatButtons.Count < interactions.Count)
            CreateChatButton(_chatButtons.Count);
        var origin = new Vector2(area.Left + 30, area.Bottom - 22 * lines + Math.Max(0, 2 * (lines - 1)) - 4);
        float slotX = origin.X, slotY = origin.Y;
        int hovered = -1;
        bool gamepad = PlayerInput.UsingGamepadUI && UILinkPointNavigator.InUse;
        _chatExtras.Clear();
        for (int i = 0; i < _chatButtons.Count; i++)
        {
            FreshValueButton button = _chatButtons[i];
            bool visible = i < interactions.Count;
            if (button.Visual.Visible != visible)
                button.Visual.Visible = visible;
            if (!visible)
                continue;
            NPCInteraction interaction = interactions[i];
            if (i % 4 == 0)
            {
                slotX = origin.X;
                slotY = origin.Y + i / 4 * 22;
            }
            string text = interaction.GetText();
            if (button.Text != text)
                button.Text = text;
            float left = MathF.Round(slotX / 2), top = MathF.Round(slotY / 2);
            // (The buttons are the box's children.)
            if (button.Visual.X != left - x || button.Visual.Y != top - ChatTop)
            {
                button.Visual.X = left - x;
                button.Visual.Y = top - ChatTop;
            }
            var slot = new Rectangle((int)(left * 2), (int)(top * 2), (int)(button.Visual.GetAbsoluteWidth() * 2), 22);
            bool over = slot.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;
            UILinkPointNavigator.SetPosition(2500 + i, slot.Center.ToVector2());
            bool focused = gamepad && UILinkPointNavigator.CurrentPoint == 2500 + i;
            button.IsLit = focused;
            if (over || focused)
            {
                hovered = i;
                Main.LocalPlayer.mouseInterface = true;
                Main.LocalPlayer.releaseUseItem = false;
                // The gamepad's A is the game's click at its cursor (the mouse clicks through Gum).
                if (focused && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    interaction.Interact();
                }
            }
            Color color = Color.White;
            bool priced = interaction.TryAddCoins(ref color, out int price);
            slotX = slot.Right + 30;
            _chatExtras.Add((priced ? price : 0, new Vector2(slotX - 45, slotY - 44), interaction.ShowExclamation,
                new Vector2(slot.Right + 8, slot.Center.Y)));
            if (priced)
                slotX += 106;
        }
        // A tick as the hover moves between buttons (the gamepad's moving onto none is silent).
        if (LastHovered(panel) != hovered && (!PlayerInput.UsingGamepad || hovered != -1))
            SoundEngine.PlaySound(12);
        SetLastHovered(panel, hovered);
    }

    private void CreateNpcChat()
    {
        _chatBox = new FreshPanel { Y = ChatTop, Width = ChatWidth };
        _chatView = new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = DrawNpcChat,
        };
        _chatBox.Children.Add(_chatView);
        // Under the HUD's game art and tooltips, over its slots.
        Children.Insert(Children.IndexOf(_over), _chatBox);
    }

    private void CreateChatButton(int index)
    {
        var button = new FreshValueButton { Color = ChatButtonColor };
        button.Visual.Height = 11;
        button.Click += (_, _) =>
        {
            if (!PlayerInput.UsingGamepadUI && _chatPanel != null && index < Interactions(_chatPanel).Count)
                Interactions(_chatPanel)[index].Interact();
        };
        _chatButtons.Add(button);
        _chatBox!.Children.Add(button.Visual);
    }

    // The game's part of the box: its text (the sign's caret and IME while editing), the portrait, the corner item, prices and notifications.
    private void DrawNpcChat(SpriteBatch batch, Rectangle area)
    {
        if (_chatPanel is not NPCChatPanel panel)
            return;
        int grey = (Main.mouseTextColor * 2 + 255) / 3;
        DrawChatText(batch, panel, area, new Color(grey, grey, grey, grey));
        Main.DrawNPCPortrait(new Color(200, 200, 200, 200), area.TopLeft());
        _chatCorner.DrawItem(batch);
        foreach ((int price, Vector2 at, bool exclamation, Vector2 exclamationAt) in _chatExtras)
        {
            if (price > 0)
                ItemSlot.DrawMoney(batch, "", at.X, at.Y, Aether.UI.CachedText.CoinsSplit(price), horizontal: true);
            if (exclamation)
                Utils.DrawNotificationIcon(batch, exclamationAt, 0f);
        }
    }

    // As NPCChatPanel.DrawText: the wrapped lines (chat tags while talking to an NPC, plain on a sign) and, while editing a sign, its caret and IME.
    private void DrawChatText(SpriteBatch batch, NPCChatPanel panel, Rectangle area, Color color)
    {
        Vector2 topLeft = area.TopLeft() + new Vector2(20f, 20f);
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        TextDisplayCache cache = TextCache(panel);
        string[] lines = cache.TextLines;
        int count = cache.AmountOfLines;
        for (int i = 0; i < count; i++)
        {
            if (lines[i] == null)
                continue;
            if (panel.allowRichText)
                _chatLines.Draw(batch, font, i, lines[i], topLeft + new Vector2(0, i * 30), color);
            else
                Utils.DrawBorderStringFourWay(batch, font, lines[i], topLeft.X, topLeft.Y + i * 30, color, Color.Black, Vector2.Zero);
        }
        if (!Main.editSign || count == 0 || lines[count - 1] == null)
            return;
        Vector2 caret = topLeft + new Vector2(font.MeasureString(lines[count - 1]).X, (count - 1) * 30);
        string composition = Platform.Get<IImeService>().CompositionString;
        if (!string.IsNullOrEmpty(composition))
        {
            float width = font.MeasureString(composition).X;
            if (width + caret.X - topLeft.X > 460f)
                caret = topLeft + new Vector2(0f, count * 30);
            ChatManager.DrawColorCodedStringWithShadow(batch, font, composition, caret, Main.imeCompositionStringColor, 0f, Vector2.Zero, Vector2.One);
            Main.instance.SetIMEPanelAnchor(caret + new Vector2(0f, 54f), 0f);
            caret.X += width;
        }
        int blinks = BlinkerCount(panel) + 1;
        if (blinks >= 20)
        {
            SetBlinkerState(panel, BlinkerState(panel) == 0 ? 1 : 0);
            blinks = 0;
        }
        SetBlinkerCount(panel, blinks);
        if (BlinkerState(panel) == 1)
            ChatManager.DrawColorCodedStringWithShadow(batch, font, "|", caret, color, 0f, Vector2.Zero, Vector2.One);
    }

    // Each line's parsed chat text, kept while the line is the same (parsing tags every frame allocates).
    private sealed class ChatTextCache
    {
        private readonly string?[] _text = new string?[16];
        private readonly TextSnippet[]?[] _snippets = new TextSnippet[]?[16];

        public void Draw(SpriteBatch batch, DynamicSpriteFont font, int line, string text, Vector2 position, Color color)
        {
            if (line >= _text.Length)
            {
                ChatManager.DrawColorCodedStringWithShadow(batch, font, text, position, color, 0f, Vector2.Zero, Vector2.One);
                return;
            }
            if (!ReferenceEquals(_text[line], text))
            {
                _text[line] = text;
                _snippets[line] = ChatManager.ParseMessage(text, color).ToArray();
            }
            TextSnippet[] snippets = _snippets[line]!;
            ChatManager.DrawColorCodedStringWithShadow(batch, font, snippets, position, color, 0f, Vector2.Zero, Vector2.One, out _);
        }
    }

    // The game's chat panel's internal state and methods.
    private static readonly FieldInfo ChatPanel = typeof(UINpcChat).GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly Action<NPCChatPanel> PrepareText = PanelCall("PrepareText");
    private static readonly Action<NPCChatPanel> PrepareInteractions = PanelCall("PrepareInteractions");
    private static readonly Action<NPCChatPanel> PrepareVirtualKeyboard = PanelCall("PrepareVirtualKeyboard");
    private static readonly Func<NPCChatPanel, List<NPCInteraction>> Interactions = PanelGet<List<NPCInteraction>>("_interactions");
    private static readonly Func<NPCChatPanel, TextDisplayCache> TextCache = PanelGet<TextDisplayCache>("_textDisplayCache");
    private static readonly Func<NPCChatPanel, int> NeededLines = PanelGet<int>("_neededInteractionLines");
    private static readonly Func<NPCChatPanel, int> LastHovered = PanelGet<int>("_lastHovered");
    private static readonly Action<NPCChatPanel, int> SetLastHovered = PanelSet<int>("_lastHovered");
    private static readonly Func<NPCChatPanel, int> BlinkerCount = PanelGet<int>("textBlinkerCount");
    private static readonly Action<NPCChatPanel, int> SetBlinkerCount = PanelSet<int>("textBlinkerCount");
    private static readonly Func<NPCChatPanel, int> BlinkerState = PanelGet<int>("textBlinkerState");
    private static readonly Action<NPCChatPanel, int> SetBlinkerState = PanelSet<int>("textBlinkerState");

    private static FieldInfo PanelField(string name) => typeof(NPCChatPanel).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static Action<NPCChatPanel> PanelCall(string name) =>
        (Action<NPCChatPanel>)Delegate.CreateDelegate(typeof(Action<NPCChatPanel>), typeof(NPCChatPanel).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!);

    private static Func<NPCChatPanel, T> PanelGet<T>(string name)
    {
        ParameterExpression panel = Expression.Parameter(typeof(NPCChatPanel));
        return Expression.Lambda<Func<NPCChatPanel, T>>(Expression.Field(panel, PanelField(name)), panel).Compile();
    }

    private static Action<NPCChatPanel, T> PanelSet<T>(string name)
    {
        ParameterExpression panel = Expression.Parameter(typeof(NPCChatPanel)), value = Expression.Parameter(typeof(T));
        return Expression.Lambda<Action<NPCChatPanel, T>>(Expression.Assign(Expression.Field(panel, PanelField(name)), value), panel, value).Compile();
    }
}
