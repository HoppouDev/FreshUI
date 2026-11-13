using Aether.Modding;
using Terraria;
using Terraria.GameContent.UI.Chat;

namespace FreshUI.Mixins;

/// <summary>
/// The chat input: editing at a cursor with selection, mouse and sent-message history
/// (<see cref="ChatInput" />), and the command completion popup (<see cref="CommandPopup" />).
/// </summary>
[Mixin(typeof(Main))]
public static class ChatMixin
{
    private const string DrawInput = "Terraria.UI.Chat.ChatManager::DrawColorCodedStringWithShadow(Microsoft.Xna.Framework.Graphics.SpriteBatch,ReLogic.Graphics.DynamicSpriteFont,Terraria.UI.Chat.TextSnippet[],Microsoft.Xna.Framework.Vector2,System.Single,Microsoft.Xna.Framework.Vector2,Microsoft.Xna.Framework.Vector2,System.Int32&,System.Single,System.Single)";

    [Inject("DoUpdate_HandleChat()", At.HEAD)]
    private static void Update(CallbackInfo callback) => CommandPopup.Update();

    // Up and Down are for suggestions and sent messages: the chat scrolls with Page Up, Page Down and the wheel.
    [Redirect("DoUpdate_HandleChat()", At.INVOKE, Target = "Terraria.GameContent.UI.Chat.IChatMonitor::Offset(System.Int32)")]
    private static void Offset(IChatMonitor monitor, int lines) => monitor.Offset(ChatInput.ScrollLines());

    // Typing goes in at the cursor, which the arrow keys and the mouse move.
    [Redirect("DoUpdate_HandleChat()", At.INVOKE, Target = "Terraria.Main::GetInputText(System.String,System.Boolean)")]
    private static string Edit(string text, bool multiLine) => ChatInput.Edit(text, multiLine);

    [Inject("DrawPlayerChat()", At.INVOKE, Target = DrawInput)]
    private static void DrawSelection(CallbackInfo callback) => ChatInput.DrawSelection(Main.spriteBatch);

    [Inject("DrawPlayerChat()", At.TAIL)]
    private static void Draw(CallbackInfo callback)
    {
        CommandPopup.Draw(Main.spriteBatch);
        ChatInput.DrawCursor(Main.spriteBatch);
    }

    // The cursor is drawn at its place in the text, after the popup, so nothing covers it.
    [ModifyConstant("DrawPlayerChat()", "string:|")]
    private static string Cursor(string cursor) => "";
}
