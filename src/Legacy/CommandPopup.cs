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
/// The Aether API's command completion (<see cref="CommandCompletion" />) for the chat input.
/// Suggestions for the word at the cursor are listed above the input (drawn by Gum,
/// <see cref="FreshUI.Screens.CommandPopupScreen"/>): Tab completes the selected one and then cycles
/// through them (Shift+Tab backwards), Up and Down select, and the mouse selects, completes and
/// scrolls. With the cursor at the end, the rest of the selected suggestion follows the typed text in
/// grey, and with nothing to suggest the usage of the next arguments or the parse error is shown instead.
/// </summary>
public static class CommandPopup
{
    internal const int MaxRows = 10;

    private static readonly Color HintColor = new(128, 128, 128);

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

    /// <summary>Where the list was last laid out, in the chat's UI units (the wheel over it scrolls it, not the chat).</summary>
    internal static Rectangle Bounds;

    private static readonly List<(string Text, bool Error)> lines = [];

    internal static bool Open => shown is { Suggestions.Count: > 0 };

    /// <summary>What the popup shows: the suggestions in view, or the usage or error lines (<c>Error</c> for an error).</summary>
    internal static IReadOnlyList<(string Text, bool Error)> Lines
    {
        get
        {
            lines.Clear();
            if (!Main.drawingPlayerChat)
                return lines;
            if (Open)
            {
                IReadOnlyList<CommandSuggestion> suggestions = shown!.Suggestions;
                for (int index = scroll; index < Math.Min(suggestions.Count, scroll + MaxRows); index++)
                    lines.Add((suggestions[index].Text, false));
            }
            else if (shown is { Hints.Count: > 0 })
            {
                foreach (CommandHint hint in shown.Hints)
                    lines.Add((hint.Text, hint.IsError));
            }
            return lines;
        }
    }

    /// <summary>The selected suggestion's row in view, or -1.</summary>
    internal static int SelectedRow => Open && selected >= scroll && selected < scroll + MaxRows ? selected - scroll : -1;

    /// <summary>The selected suggestion's description, while it's in view.</summary>
    internal static string? SelectedTooltip => SelectedRow >= 0 && shown!.Suggestions[selected].Tooltip is { Length: > 0 } tooltip ? tooltip : null;

    /// <summary>Where in the input (UI units from its text's start) the completed word, or the usage, starts.</summary>
    internal static float AnchorX
    {
        get
        {
            if (shown == null)
                return 0;
            int position = Math.Clamp(Open ? shown.Start : shown.HintStart, 0, shownInput.Length);
            return ChatInput.Width(FontAssets.MouseText.Value, shownInput[..position]);
        }
    }

    /// <summary>The mouse came onto a row in view: it's selected.</summary>
    internal static void Hover(int row)
    {
        if (Open && scroll + row < shown!.Suggestions.Count)
            selected = scroll + row;
    }

    /// <summary>A row in view was clicked: it's completed.</summary>
    internal static void Click(int row)
    {
        if (!Open || scroll + row >= shown!.Suggestions.Count)
            return;
        selected = scroll + row;
        Complete();
    }

    /// <summary>The wheel over the list: up (-1) or down (1) a row.</summary>
    internal static void ScrollBy(int rows)
    {
        if (Open)
            scroll = Math.Clamp(scroll + rows, 0, Math.Max(0, shown!.Suggestions.Count - MaxRows));
    }

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
        // The list scrolls itself (Gum); the chat doesn't.
        if (Bounds.Contains(PlayerInput.UIMousePosition.ToPoint()))
            PlayerInput.ScrollWheelDelta = 0;
    }

    /// <summary>With the cursor at the end, the rest of the selected suggestion after the typed text, in grey (in UI coordinates, with the input's text).</summary>
    public static void DrawGhost(SpriteBatch batch)
    {
        if (!Main.drawingPlayerChat)
            return;
        Refresh();
        if (!Open || cycling || inputCaret != Main.chatText.Length)
            return;
        CommandSuggestion current = shown!.Suggestions[selected];
        int typedEnd = Math.Min(current.End, shownInput.Length);
        string typed = shownInput[Math.Min(current.Start, typedEnd)..typedEnd];
        if (current.Text.Length > typed.Length && current.Text.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            ChatManager.DrawColorCodedStringWithShadow(batch, font, [new TextSnippet(current.Text[typed.Length..], HintColor) { UseRawColor = true }],
                new Vector2(ChatInput.InputX + ChatInput.Width(font, Main.chatText), Main.screenHeight - 30), 0f, Vector2.Zero, Vector2.One, out _);
        }
    }

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
}
