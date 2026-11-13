using Aether.Api;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using ReLogic.Localization.IME;
using ReLogic.OS;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI.Chat;

namespace FreshUI.Legacy;

/// <summary>
/// Shows the Aether API's command completion (<see cref="CommandCompletion" />) for the chat input.
/// Suggestions for the word at the cursor are listed above the input: Tab completes the selected
/// one and then cycles through them (Shift+Tab backwards), Up and Down select, and the mouse
/// selects, completes and scrolls. With the cursor at the end, the rest of the selected suggestion
/// follows the typed text in grey, and with nothing to suggest the usage of the next arguments or
/// the parse error is shown instead.
/// </summary>
public static class CommandPopup
{
    private const int MaxRows = 10;
    private const int RowHeight = 24;
    private const int TextOffset = 3;
    private const int Padding = 4;

    private static readonly Color TextColor = new(170, 170, 170);
    private static readonly Color ErrorColor = new(255, 85, 85);
    private static readonly Color SelectedColor = new(255, 255, 85);
    private static readonly Color HintColor = new(128, 128, 128);
    private static readonly Color Background = new(0, 0, 0, 208);

    // The chat text and cursor the state is for, those the last completion produced (its list stays),
    // and those not to complete (a recalled message).
    private static string? input;
    private static int inputCaret;
    private static string? completed;
    private static int completedCaret;
    private static string? hidden;
    private static int hiddenCaret;

    private static Task<CommandCompletions>? pending;
    private static string pendingInput = "";

    // What's shown, for the chat text it completes.
    private static CommandCompletions? shown;
    private static string shownInput = "";
    private static int selected;
    private static int scroll;
    private static bool cycling;

    // The list as last drawn, and the mouse then.
    private static Rectangle bounds;
    private static Point lastMouse;

    internal static bool Open => shown is { Suggestions.Count: > 0 };

    /// <summary>Completes nothing for this text and cursor (until either changes).</summary>
    internal static void Hide(string text, int caret)
    {
        hidden = text;
        hiddenCaret = caret;
    }

    /// <summary>Handles the completion keys and the wheel over the list (before the game reads the chat input).</summary>
    public static void Update()
    {
        Refresh();
        if (!Open || Main.CurrentInputTextTakerOverride != null || Platform.Get<IImeService>().CompositionString is { Length: > 0 })
            return;
        bool shift = Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift);
        if (Pressed(Keys.Tab))
        {
            if (cycling)
                Select(selected + (shift ? -1 : 1));
            Complete();
        }
        else if (Pressed(Keys.Up) || Pressed(Keys.Down))
        {
            Select(selected + (Pressed(Keys.Up) ? -1 : 1));
            if (cycling)
                Complete();
        }
        if (PlayerInput.ScrollWheelDelta != 0 && bounds.Contains(PlayerInput.UIMousePosition.ToPoint()))
        {
            scroll = Math.Clamp(scroll - Math.Sign(PlayerInput.ScrollWheelDelta), 0, Math.Max(0, shown!.Suggestions.Count - MaxRows));
            PlayerInput.ScrollWheelDelta = 0;
        }
    }

    /// <summary>Draws the suggestions, or the usage or error, above the chat input (in UI coordinates).</summary>
    public static void Draw(SpriteBatch batch)
    {
        bounds = Rectangle.Empty;
        if (!Main.drawingPlayerChat)
            return;
        Refresh();
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        int bottom = Main.screenHeight - 38;
        if (Open)
            DrawList(batch, font, bottom);
        else if (shown is { Hints.Count: > 0 })
            DrawHints(batch, font, bottom);
    }

    private static void DrawList(SpriteBatch batch, DynamicSpriteFont font, int bottom)
    {
        IReadOnlyList<CommandSuggestion> suggestions = shown!.Suggestions;
        CommandSuggestion current = suggestions[selected];
        int typedEnd = Math.Min(current.End, shownInput.Length);
        string typed = shownInput[Math.Min(current.Start, typedEnd)..typedEnd];
        if (!cycling && inputCaret == Main.chatText.Length && current.Text.Length > typed.Length &&
            current.Text.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
        {
            DrawText(batch, font, current.Text[typed.Length..],
                new Vector2(ChatInput.InputX + ChatInput.Width(font, Main.chatText), Main.screenHeight - 30), HintColor);
        }

        int rows = Math.Min(suggestions.Count, MaxRows);
        int width = 0;
        foreach (CommandSuggestion suggestion in suggestions)
            width = Math.Max(width, (int)Math.Ceiling(font.MeasureString(suggestion.Text).X));
        width += Padding * 2;
        int x = Left(font, shownInput, shown.Start, width);
        int top = bottom - rows * RowHeight;
        bounds = new Rectangle(x, top, width, rows * RowHeight);
        Fill(batch, bounds, Background);

        Point mouse = PlayerInput.UIMousePosition.ToPoint();
        if (bounds.Contains(mouse))
        {
            Main.LocalPlayer.mouseInterface = true;
            int hovered = scroll + (mouse.Y - top) / RowHeight;
            if (mouse != lastMouse)
                selected = hovered;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                selected = hovered;
                Complete();
            }
        }
        lastMouse = mouse;

        for (int row = 0; row < rows; row++)
        {
            int index = scroll + row;
            DrawText(batch, font, suggestions[index].Text, new Vector2(x + Padding, top + row * RowHeight + TextOffset),
                index == selected ? SelectedColor : TextColor);
        }
        // Dotted edges where the list continues.
        if (scroll > 0)
            Dots(batch, x, top - 2, width);
        if (scroll + rows < suggestions.Count)
            Dots(batch, x, bounds.Bottom, width);

        if (suggestions[selected].Tooltip is { Length: > 0 } tooltip && selected >= scroll && selected < scroll + rows)
        {
            int tooltipWidth = (int)Math.Ceiling(font.MeasureString(tooltip).X) + Padding * 2;
            int tooltipX = Math.Min(bounds.Right + 2, Math.Max(0, Main.screenWidth - tooltipWidth));
            var box = new Rectangle(tooltipX, top + (selected - scroll) * RowHeight, tooltipWidth, RowHeight);
            Fill(batch, box, Background);
            DrawText(batch, font, tooltip, new Vector2(box.X + Padding, box.Y + TextOffset), Color.White);
        }
    }

    private static void DrawHints(SpriteBatch batch, DynamicSpriteFont font, int bottom)
    {
        IReadOnlyList<CommandHint> hints = shown!.Hints;
        int width = 0;
        foreach (CommandHint hint in hints)
            width = Math.Max(width, (int)Math.Ceiling(font.MeasureString(hint.Text).X));
        width += Padding * 2;
        int x = Left(font, shownInput, shown.HintStart, width);
        int top = bottom - hints.Count * RowHeight;
        Fill(batch, new Rectangle(x, top, width, hints.Count * RowHeight), Background);
        for (int line = 0; line < hints.Count; line++)
        {
            DrawText(batch, font, hints[line].Text, new Vector2(x + Padding, top + line * RowHeight + TextOffset),
                hints[line].IsError ? ErrorColor : TextColor);
        }
    }

    // A box's left edge: under the given place in the input, kept on screen.
    private static int Left(DynamicSpriteFont font, string text, int position, int width) =>
        Math.Clamp(ChatInput.InputX + (int)ChatInput.Width(font, text[..Math.Clamp(position, 0, text.Length)]) - Padding, 0,
            Math.Max(0, Main.screenWidth - width));

    // Brings the state up to the chat text and cursor: completes them again unless a completion or a
    // recalled message wrote them.
    private static void Refresh()
    {
        string text = Main.drawingPlayerChat ? Main.chatText : "";
        int caret = ChatInput.Caret;
        if (text == input && caret == inputCaret)
        {
            Poll();
            return;
        }
        input = text;
        inputCaret = caret;
        bool own = text == completed && caret == completedCaret;
        bool hide = text == hidden && caret == hiddenCaret;
        completed = null;
        if (!hide)
            hidden = null;
        if (own)
            return;
        pending = null;
        shown = null;
        cycling = false;
        selected = scroll = 0;
        if (!hide && text.StartsWith('/'))
        {
            pending = CommandCompletion.Complete(text, caret);
            pendingInput = text;
        }
        Poll();
    }

    // Takes the completions once they're ready (a mod's provider can be asynchronous).
    private static void Poll()
    {
        if (pending is not { IsCompleted: true } task)
            return;
        pending = null;
        shown = task.IsCompletedSuccessfully ? task.Result : null;
        shownInput = pendingInput;
        selected = scroll = 0;
    }

    private static void Select(int index)
    {
        int count = shown!.Suggestions.Count;
        selected = (index % count + count) % count;
        if (selected < scroll)
            scroll = selected;
        else if (selected >= scroll + MaxRows)
            scroll = selected - MaxRows + 1;
    }

    // Replaces the word up to the cursor, leaving the cursor after the suggestion.
    private static void Complete()
    {
        CommandSuggestion suggestion = shown!.Suggestions[selected];
        completedCaret = suggestion.Start + suggestion.Text.Length;
        ChatInput.SetText(suggestion.Apply(shownInput), completedCaret);
        completed = Main.chatText;
        cycling = true;
        SoundEngine.PlaySound(12);
    }

    private static bool Pressed(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);

    private static void DrawText(SpriteBatch batch, DynamicSpriteFont font, string text, Vector2 position, Color color) =>
        ChatManager.DrawColorCodedStringWithShadow(batch, font, [new TextSnippet(text, color) { UseRawColor = true }],
            position, 0f, Vector2.Zero, Vector2.One, out _);

    private static void Fill(SpriteBatch batch, Rectangle rectangle, Color color) =>
        batch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);

    private static void Dots(SpriteBatch batch, int x, int y, int width)
    {
        for (int dot = 0; dot < width; dot += 4)
            Fill(batch, new Rectangle(x + dot, y, 2, 2), TextColor);
    }
}
