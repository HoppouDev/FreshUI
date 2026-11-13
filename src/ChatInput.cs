using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using ReLogic.OS;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI.Chat;

namespace FreshUI;

/// <summary>
/// Editing in the chat input. Left and Right move the cursor (by words with Ctrl, Home and End to
/// the ends) and Shift extends a selection, which Ctrl+A makes of all the text. A click places the
/// cursor (Shift+click extends the selection), dragging selects and a double click selects a word.
/// Ctrl+C, Ctrl+X and Ctrl+V copy, cut and paste (the selection, or everything without one, as
/// Terraria did), Delete removes forwards, and with Ctrl, Backspace and Delete remove words. Up and
/// Down go through the messages sent before (the text being written comes back after the newest),
/// so the chat scrolls with Page Up, Page Down and the wheel instead.
/// Terraria still reads what's typed and Backspace, but given only the text before the cursor, so
/// typing goes in at the cursor; its own shortcuts are hidden from it. The cursor stays solid while
/// the text or the cursor changes and blinks again once they don't.
/// </summary>
public static class ChatInput
{
    internal const int InputX = 88;

    private const int RepeatDelay = 30;
    private const int RepeatInterval = 2;
    private const double BlinkSeconds = 20 / 60.0;
    private const double DoubleClickSeconds = 0.4;
    private const int MaxHistory = 100;
    private const int WheelLines = 3;

    // Premultiplied blue, behind the white text.
    private static readonly Color SelectionColor = new(26, 77, 153, 160);

    // The chat text the cursor is for, the cursor, and the selection's other end (the cursor without one).
    private static string text = "";
    private static int caret;
    private static int anchor;

    private static long lastChange;
    private static Keys repeatKey;
    private static int repeatTicks;

    // The input as last drawn; a drag selecting from the anchor; the last click, for double clicks.
    private static Rectangle bounds;
    private static bool dragging;
    private static long lastClick;
    private static int lastClickIndex = -1;

    // Sent messages, oldest first; while going through them, which one and the text being written.
    private static readonly List<string> history = [];
    private static int historyIndex = -1;
    private static string draft = "";

    public static int Caret
    {
        get
        {
            Sync();
            return caret;
        }
    }

    /// <summary>Replaces the text (a completion), with the cursor at <paramref name="cursor" />.</summary>
    public static void SetText(string value, int cursor)
    {
        Main.chatText = text = value;
        caret = anchor = Math.Clamp(cursor, 0, value.Length);
        Changed();
    }

    /// <summary>Stands in for Terraria's <c>GetInputText</c> on the chat input: edits at the cursor.</summary>
    public static string Edit(string current, bool multiLine)
    {
        Sync();
        HandleMouse();
        // Backspace takes the selection when there is one; Terraria then sees Backspace as already held.
        bool backspace = anchor != caret && Pressed(Keys.Back);
        if (backspace)
            Replace(Math.Min(anchor, caret), Math.Max(anchor, caret), "", Math.Min(anchor, caret));
        else
            HandleKeys();
        int start = Math.Min(anchor, caret), end = Math.Max(anchor, caret);
        string before = text[..start];
        IEnumerable<Keys> held = Main.inputText.GetPressedKeys().Where(key => key is not
            (Keys.LeftControl or Keys.RightControl or Keys.Insert or Keys.Delete));
        Main.inputText = new KeyboardState((backspace ? held.Append(Keys.Back) : held).ToArray());
        string edited = Main.GetInputText(before, multiLine);
        // With Ctrl, Backspace removes words here, not Terraria's single character.
        if (Control() && edited.Length < before.Length && before.StartsWith(edited, StringComparison.Ordinal))
            edited = before;
        if (edited != before)
            Replace(0, end, edited, edited.Length);
        // Enter sends it (as Terraria goes on to do).
        if (Main.inputTextEnter && Main.chatRelease && text.Length > 0)
            Remember(text);
        return text;
    }

    /// <summary>Draws the selection behind the input's text (before Terraria draws it).</summary>
    public static void DrawSelection(SpriteBatch batch)
    {
        Sync();
        // Terraria's input box, which clicks there are for rather than the world.
        int width = Main.screenWidth > 800 ? Main.screenWidth - 300 : TextureAssets.TextBack.Width();
        bounds = new Rectangle(InputX - 10, Main.screenHeight - 36, width, TextureAssets.TextBack.Height());
        if (bounds.Contains(PlayerInput.UIMousePosition.ToPoint()))
            Main.LocalPlayer.mouseInterface = true;
        if (anchor == caret)
            return;
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        int start = InputX + (int)Width(font, text[..Math.Min(anchor, caret)]);
        int end = InputX + (int)Width(font, text[..Math.Max(anchor, caret)]);
        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(start, Main.screenHeight - 32, end - start, 26), SelectionColor);
    }

    /// <summary>Draws the cursor over the text and whatever follows it.</summary>
    public static void DrawCursor(SpriteBatch batch)
    {
        Sync();
        if (!Main.drawingPlayerChat || (int)(Stopwatch.GetElapsedTime(lastChange).TotalSeconds / BlinkSeconds) % 2 != 0)
            return;
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        // Any IME composition is drawn after the text: the cursor follows it there.
        string composition = caret == text.Length ? Platform.Get<ReLogic.Localization.IME.IImeService>().CompositionString ?? "" : "";
        // Centred on the gap between characters rather than over the next one.
        float x = InputX + Width(font, text[..caret] + composition) - MathF.Round(font.MeasureString("|").X / 2f);
        var position = new Vector2(x, Main.screenHeight - 30);
        ChatManager.DrawColorCodedStringWithShadow(batch, font, [new TextSnippet("|", Color.White)], position, 0f,
            Vector2.Zero, Vector2.One, out _);
    }

    /// <summary>How far the chat scrolls this update: Page Up and Page Down while held, and the wheel.</summary>
    public static int ScrollLines()
    {
        int lines = Main.keyState.IsKeyDown(Keys.PageUp) ? 1 : Main.keyState.IsKeyDown(Keys.PageDown) ? -1 : 0;
        if (PlayerInput.ScrollWheelDelta != 0)
        {
            lines += PlayerInput.ScrollWheelDelta / 120 * WheelLines;
            PlayerInput.ScrollWheelDelta = 0;
        }
        return lines;
    }

    internal static float Width(DynamicSpriteFont font, string value) => ChatManager.GetStringSize(font, value, Vector2.One).X;

    // Follows text set elsewhere (opening the chat, an item linked in): the cursor goes to the end.
    private static void Sync()
    {
        string current = Main.drawingPlayerChat ? Main.chatText : "";
        if (current == text)
            return;
        text = current;
        caret = anchor = text.Length;
        historyIndex = -1;
        dragging = false;
        Changed();
    }

    private static void HandleKeys()
    {
        KeyboardState keys = Main.keyState;
        bool control = Control();
        bool shift = Shift();
        int start = Math.Min(anchor, caret), end = Math.Max(anchor, caret);
        if (control && Pressed(Keys.A))
        {
            anchor = 0;
            caret = text.Length;
            Changed();
        }
        else if (control && (Pressed(Keys.C) || Pressed(Keys.Insert)))
        {
            Platform.Get<IClipboard>().Value = Selected();
        }
        else if ((control && Pressed(Keys.X)) || (shift && Pressed(Keys.Delete)))
        {
            Platform.Get<IClipboard>().Value = Selected();
            if (start == end)
                Replace(0, text.Length, "", 0);
            else
                Replace(start, end, "", start);
        }
        else if ((control && Pressed(Keys.V)) || (shift && Pressed(Keys.Insert)))
        {
            string pasted = Platform.Get<IClipboard>().Value ?? "";
            Replace(start, end, pasted, start + pasted.Length);
        }
        else if (control && Pressed(Keys.Z))
        {
            Replace(0, text.Length, "", 0);
        }
        else if (control && Repeating(Keys.Back))
        {
            if (start != end)
                Replace(start, end, "", start);
            else
                Replace(Word(caret, -1), caret, "", Word(caret, -1));
        }
        else if (Repeating(Keys.Delete))
        {
            if (start != end)
                Replace(start, end, "", start);
            else if (caret < text.Length)
                Replace(caret, control ? Word(caret, 1) : Step(caret, 1), "", caret);
        }
        else if (!CommandPopup.Open && (Repeating(Keys.Up) || Repeating(Keys.Down)))
        {
            Recall(keys.IsKeyDown(Keys.Up) ? -1 : 1);
        }
        else if (Repeating(Keys.Left) || Repeating(Keys.Right) || Pressed(Keys.Home) || Pressed(Keys.End))
        {
            int target = keys.IsKeyDown(Keys.Home) ? 0
                : keys.IsKeyDown(Keys.End) ? text.Length
                : keys.IsKeyDown(Keys.Left) ? !shift && start != end ? start : control ? Word(caret, -1) : Step(caret, -1)
                : !shift && start != end ? end : control ? Word(caret, 1) : Step(caret, 1);
            caret = target;
            if (!shift)
                anchor = caret;
            Changed();
        }
    }

    private static void HandleMouse()
    {
        Point mouse = PlayerInput.UIMousePosition.ToPoint();
        if (!Main.mouseLeft)
        {
            dragging = false;
            return;
        }
        if (dragging)
        {
            MoveTo(IndexAt(mouse.X), select: true);
            return;
        }
        if (!Main.mouseLeftRelease || !bounds.Contains(mouse))
            return;
        int index = IndexAt(mouse.X);
        if (index == lastClickIndex && Stopwatch.GetElapsedTime(lastClick).TotalSeconds < DoubleClickSeconds)
        {
            // The word clicked, from its start to its end.
            anchor = index;
            while (anchor > 0 && text[anchor - 1] != ' ')
                anchor--;
            caret = index;
            while (caret < text.Length && text[caret] != ' ')
                caret++;
            Changed();
            lastClickIndex = -1;
            return;
        }
        MoveTo(index, Shift());
        dragging = true;
        lastClick = Stopwatch.GetTimestamp();
        lastClickIndex = index;
    }

    private static void MoveTo(int index, bool select)
    {
        if (index == caret && (select || anchor == caret))
            return;
        caret = index;
        if (!select)
            anchor = caret;
        Changed();
    }

    // The place between characters (or around a chat tag) nearest x.
    private static int IndexAt(int x)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        int best = 0;
        float distance = float.MaxValue;
        for (int index = 0; ; index = Step(index, 1))
        {
            float gap = Math.Abs(InputX + Width(font, text[..index]) - x);
            if (gap < distance)
            {
                best = index;
                distance = gap;
            }
            if (index == text.Length)
                return best;
        }
    }

    private static void Remember(string message)
    {
        if (history.Count == 0 || history[^1] != message)
            history.Add(message);
        if (history.Count > MaxHistory)
            history.RemoveAt(0);
        historyIndex = -1;
    }

    // Up (-1) to older messages, Down (1) to newer ones and then the text being written.
    private static void Recall(int direction)
    {
        if (history.Count == 0)
            return;
        int index = historyIndex < 0 ? history.Count : historyIndex;
        int next = Math.Clamp(index + direction, 0, history.Count);
        if (next == index)
            return;
        if (historyIndex < 0)
            draft = text;
        string value = next == history.Count ? draft : history[next];
        SetText(value, value.Length);
        historyIndex = next == history.Count ? -1 : next;
        // A recalled command isn't completed until it's edited.
        CommandPopup.Hide(value, value.Length);
    }

    private static bool Control() =>
        (Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl)) &&
        !Main.keyState.IsKeyDown(Keys.LeftAlt) && !Main.keyState.IsKeyDown(Keys.RightAlt);

    private static bool Shift() => Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);

    private static string Selected() => anchor == caret ? text : text[Math.Min(anchor, caret)..Math.Max(anchor, caret)];

    // Puts value in place of [start, end), and the cursor at cursor.
    private static void Replace(int start, int end, string value, int cursor)
    {
        text = text[..start] + value + text[end..];
        Main.chatText = text;
        caret = anchor = Math.Clamp(cursor, 0, text.Length);
        historyIndex = -1;
        Changed();
    }

    private static void Changed() => lastChange = Stopwatch.GetTimestamp();

    // One character along, or over a whole chat tag ([i:123]) as Backspace removes one.
    private static int Step(int position, int direction)
    {
        int next = Math.Clamp(position + direction, 0, text.Length);
        foreach (System.Text.RegularExpressions.Match tag in ChatManager.Regexes.Format.Matches(text))
        {
            if (next > tag.Index && next < tag.Index + tag.Length)
                return direction < 0 ? tag.Index : tag.Index + tag.Length;
        }
        return next;
    }

    // To the start of this or the previous word, or past the end of this or the next.
    private static int Word(int position, int direction)
    {
        if (direction < 0)
        {
            while (position > 0 && text[position - 1] == ' ')
                position--;
            while (position > 0 && text[position - 1] != ' ')
                position--;
        }
        else
        {
            while (position < text.Length && text[position] == ' ')
                position++;
            while (position < text.Length && text[position] != ' ')
                position++;
        }
        return position;
    }

    private static bool Pressed(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);

    // Pressed, then again after a delay while held.
    private static bool Repeating(Keys key)
    {
        if (!Main.keyState.IsKeyDown(key))
            return false;
        if (!Main.oldKeyState.IsKeyDown(key))
        {
            repeatKey = key;
            repeatTicks = RepeatDelay;
            return true;
        }
        if (key != repeatKey || --repeatTicks > 0)
            return false;
        repeatTicks = RepeatInterval;
        return true;
    }
}
