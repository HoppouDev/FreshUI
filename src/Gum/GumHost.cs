using FreshUI.Components;
using System.Reflection;
using Aether.Api;
using Aether.Testing;
using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
using Terraria;
using Terraria.UI;

namespace FreshUI.GumUi;

/// <summary>
/// Runs Gum inside the game: one retained mod UI layer (<c>fresh-ui:gum</c>, Aether's
/// <c>RetainedUi</c>) whose surface updates Gum every update and draws it every frame, over the HUD
/// and under mouse text and the cursor. Gum's canvas is the window in UI pixels at FreshUI's GUI scale
/// (<see cref="UiPixels" />, following <see cref="GuiScale.Current" />), its text is FreshUI's
/// distance-field text (<see cref="GumText" />), and its textures are the game's (<see cref="GumTextures" />).
/// The surface takes the mouse only over an interactive Gum element, so elsewhere the game gets it.
/// Gum reads FNA's input, so Aether's synthetic input (remote automation) reaches it too.
/// </summary>
public static class GumHost
{
    public const string LayerId = "fresh-ui:gum";

    // SpriteBatch's Begin arguments, to restart the game's batch as it was after Gum draws.
    private static readonly FieldInfo[] BatchState = new[]
    {
        "sortMode", "blendState", "samplerState", "depthStencilState", "rasterizerState", "customEffect", "transformMatrix"
    }.Select(name => typeof(SpriteBatch).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!).ToArray();

    // FNA's Game.IsActive, set without its events (see Update).
    private static readonly FieldInfo GameIsActive =
        typeof(Game).GetField("INTERNAL_isActive", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Whether Gum has been set up (on the first frame in a world).</summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>Registers the layer and the automation tree; from the mod initializer.</summary>
    public static void Install()
    {
        RetainedUi.Register(LayerId, () => new GumState());
        UiTrees.Register(new GumUiTree());
    }

    private static void Initialize()
    {
        IsInitialized = true;
        // The game's textures (and GumText's shader) are premultiplied.
        Renderer.UsePremultipliedAlpha = true;
        GumService.Default.Initialize(Main.instance);
        FreshButton.BackingPath = "Images/UI/ButtonBacking";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Inventory] = "Images/Inventory_Back";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Hotbar] = "Images/Inventory_Back";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Coin] = "Images/Inventory_Back6";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Ammo] = "Images/Inventory_Back8";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Trash] = "Images/Inventory_Back7";
        FreshItemSlot.HighlightOverlayPath = "Images/Inventory_Back22";
        // The thin gold outline the crafting window draws round its selected recipe.
        FreshItemSlot.SelectedOverlayPath = "Images/Inventory_Back24";
        UiPixels.SetScale(GuiScale.Current);
        GumText.Initialize(Main.instance.GraphicsDevice);
    }

    /// <summary>
    /// Sets Gum up and builds the screens, once: from the first update or draw in a world, whichever
    /// comes first, so the first frame's HUD is already Gum's.
    /// </summary>
    internal static void EnsureReady()
    {
        if (!IsInitialized)
        {
            Initialize();
        }
        foreach (GumScreen screen in GumScreens.All)
        {
            screen.Build();
        }
    }

    private static void Update(GameTime time)
    {
        EnsureReady();
        if (GuiScale.Current != UiPixels.Scale)
        {
            UiPixels.SetScale(GuiScale.Current);
        }
        // Gum ignores input while the window is inactive. Under remote automation, which drives an
        // unfocused window, the window counts as focused, as Aether's patch to FocusHelper makes it for
        // the game.
        bool lendFocus = SyntheticInput.Enabled && !Main.instance.IsActive;
        if (lendFocus)
        {
            GameIsActive.SetValue(Main.instance, true);
        }
        try
        {
            GumService.Default.Update(time);
        }
        finally
        {
            if (lendFocus)
            {
                GameIsActive.SetValue(Main.instance, false);
            }
        }
    }

    private static void Draw(SpriteBatch batch)
    {
        if (!IsInitialized)
        {
            return;
        }
        FreshUI.Screens.HudScreen.EndFrame();
        object?[] state = BatchState.Select(field => field.GetValue(batch)).ToArray();
        batch.End();
        try
        {
            GumService.Default.Draw();
        }
        finally
        {
            batch.Begin((SpriteSortMode)state[0]!, (BlendState?)state[1], (SamplerState?)state[2], (DepthStencilState?)state[3],
                (RasterizerState?)state[4], (Effect?)state[5], (Matrix)state[6]!);
        }
    }

    // Over an element that takes input (a control, or anything with events), not empty canvas.
    private static bool IsOverGum() =>
        IsInitialized && FrameworkElement.MainCursor.VisualOver is InteractiveGue over && over != GumService.Default.Root;

    private sealed class GumState : UIState
    {
        public override void OnInitialize() => Append(new GumSurface());
    }

    /// <summary>The layer's one element: the whole screen, for drawing, but only Gum's elements for the mouse.</summary>
    private sealed class GumSurface : UIElement
    {
        public GumSurface()
        {
            Width.Set(0, 1);
            Height.Set(0, 1);
        }

        public override bool ContainsPoint(Vector2 point) => IsOverGum();

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            GumHost.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) => GumHost.Draw(spriteBatch);
    }
}
