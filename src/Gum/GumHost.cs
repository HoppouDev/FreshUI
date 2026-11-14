using FreshUI.Components;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Aether.Api;
using Aether.Testing;
using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
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

    private static readonly FieldInfo BatchBegun = typeof(SpriteBatch).GetField("beginCalled", BindingFlags.NonPublic | BindingFlags.Instance)!;

    // FNA's Game.IsActive, set without its events (see Update).
    private static readonly FieldInfo GameIsActive =
        typeof(Game).GetField("INTERNAL_isActive", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Whether Gum has been set up (on the first frame in a world).</summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Screen pixels per UI pixel in place of <see cref="GuiScale.Current"/> (the widget gallery's
    /// scale), or null to follow the GUI scale setting.
    /// </summary>
    public static int? ScaleOverride { get; set; }

    /// <summary>
    /// Whether Gum's controls take the gamepad: the D-pad and left stick move the focus (Gum's own
    /// navigation), A activates, a focused slider steps and a scroll area scrolls with the right stick.
    /// On for the widget gallery; in a world the HUD's slots are the game's own gamepad UI.
    /// </summary>
    public static bool GamepadEnabled
    {
        get => FrameworkElement.GamePadsForUiControl.Count > 0;
        set
        {
            if (value == GamepadEnabled)
                return;
            FrameworkElement.GamePadsForUiControl.Clear();
            if (value)
                GumService.Default.UseGamepadDefaults();
        }
    }

    // Animations stepped every frame before Gum draws, by their owner (held weakly), while they run.
    private static readonly ConditionalWeakTable<object, Func<float, bool>> Animations = new();
    private static readonly List<object> Finished = [];
    private static long _lastFrame;

    /// <summary>
    /// Steps <paramref name="step"/> every frame before Gum draws, with the seconds since the last
    /// frame, until it returns false; one animation per <paramref name="owner"/>, replacing any before.
    /// </summary>
    public static void Animate(object owner, Func<float, bool> step) => Animations.AddOrUpdate(owner, step);

    private static void StepAnimations()
    {
        long now = Stopwatch.GetTimestamp();
        float seconds = _lastFrame == 0 ? 0f : MathF.Min(0.1f, (now - _lastFrame) / (float)Stopwatch.Frequency);
        _lastFrame = now;
        foreach ((object owner, Func<float, bool> step) in Animations)
        {
            if (!step(seconds))
                Finished.Add(owner);
        }
        foreach (object owner in Finished)
            Animations.Remove(owner);
        Finished.Clear();
    }

    /// <summary>The Gum layer of menus, drawn over the HUD (see <see cref="GumLayer.Overlay"/>).</summary>
    public static Layer OverlayLayer { get; private set; } = null!;

    /// <summary>
    /// The overlay screens' parent: a root of its own on <see cref="OverlayLayer"/> (Gum's main root's
    /// children draw with it, on the main layer), updated (input) with the main root, after it, so it's on top.
    /// </summary>
    public static ContainerRuntime OverlayRoot { get; private set; } = null!;

    private static readonly List<GraphicalUiElement> Roots = [];
    private static readonly Matrix OffCanvas = Matrix.CreateTranslation(-1e6f, -1e6f, 0);

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
        OverlayLayer = SystemManagers.Default.Renderer.AddLayer();
        OverlayRoot = new ContainerRuntime { Name = "Overlay Root" };
        OverlayRoot.AddToManagers(SystemManagers.Default, OverlayLayer);
        FreshButton.BackingPath = "Images/UI/ButtonBacking";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Inventory] = "Images/Inventory_Back";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Hotbar] = "Images/Inventory_Back";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Coin] = "Images/Inventory_Back6";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Ammo] = "Images/Inventory_Back8";
        FreshItemSlot.BackgroundPaths[FreshItemSlotKind.Trash] = "Images/Inventory_Back7";
        FreshItemSlot.HighlightOverlayPath = "Images/Inventory_Back22";
        // The thin gold outline the crafting window draws round its selected recipe.
        FreshItemSlot.SelectedOverlayPath = "Images/Inventory_Back24";
        UiPixels.SetScale(ScaleOverride ?? GuiScale.Current);
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

    // Whether Gum has updated since the last game update ended (UpdateIfIdle).
    private static bool _updated;

    /// <summary>
    /// After each game update: Gum's update, if the HUD's layer didn't run it (the game suspends that
    /// layer on the title menu, in the pause menu and settings, in fancy UIs and on the full map, where
    /// overlay screens still need it). Not before the game has loaded.
    /// </summary>
    public static void UpdateIfIdle(GameTime time)
    {
        if (!_updated && !Main.showSplash && !Main.dedServ)
        {
            Update(time);
        }
        _updated = false;
    }

    /// <summary>Updates Gum (layout, input) once per game update.</summary>
    internal static void Update(GameTime time)
    {
        _updated = true;
        EnsureReady();
        foreach (GumScreen screen in GumScreens.All)
        {
            screen.Sync();
            if (screen.IsShown && screen.Root is IGumScreenUpdate updating)
            {
                updating.UpdateScreen();
            }
        }
        int scale = ScaleOverride ?? GuiScale.Current;
        if (scale != UiPixels.Scale)
        {
            UiPixels.SetScale(scale);
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
            // As GumService.Update(time) does for its root: each root covers the canvas.
            foreach (GraphicalUiElement root in new GraphicalUiElement[] { GumService.Default.Root, OverlayRoot })
            {
                root.X = root.Y = 0;
                root.WidthUnits = root.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                root.Width = GraphicalUiElement.CanvasWidth;
                root.Height = GraphicalUiElement.CanvasHeight;
            }
            // While the gamepad drives a menu, the mouse (left wherever it was) hovers nothing: the focus is what's lit.
            if (FrameworkElement.MainCursor is Gum.Input.Cursor cursor)
                cursor.TransformMatrix = GamepadEnabled && Terraria.GameInput.PlayerInput.UsingGamepadUI ? OffCanvas : Matrix.Identity;
            Roots.Clear();
            Roots.Add(GumService.Default.Root);
            Roots.Add(OverlayRoot);
            GumService.Default.Update(time, Roots);
        }
        finally
        {
            if (lendFocus)
            {
                GameIsActive.SetValue(Main.instance, false);
            }
        }
        GumGamepad.Update();
    }

    /// <summary>Draws the HUD's Gum layer within the game's sprite batch (the retained layer, in a world).</summary>
    public static void Draw(SpriteBatch batch)
    {
        if (!IsInitialized)
        {
            return;
        }
        FreshUI.Screens.HudScreen.EndFrame();
        DrawLayer(batch, SystemManagers.Default.Renderer.MainLayer);
    }

    // Whether the overlay has drawn this frame (it's drawn at the first of its draw points reached).
    private static bool _overlayDrawn;

    /// <summary>A frame starts: the overlay hasn't drawn yet.</summary>
    public static void BeginFrame() => _overlayDrawn = false;

    /// <summary>Draws the overlay's Gum layer (menus) within the game's sprite batch, once a frame: on the title menu and in game, under mouse text and the cursor.</summary>
    public static void DrawOverlay(SpriteBatch batch)
    {
        if (IsInitialized && !_overlayDrawn && (bool)BatchBegun.GetValue(batch)!)
        {
            _overlayDrawn = true;
            // Over an overlay screen (a menu, or a modal one's dimmed backdrop), the game takes no clicks.
            if (!Main.gameMenu && IsOverGum())
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            DrawLayer(batch, OverlayLayer);
        }
    }

    // One layer, ending the game's sprite batch and restarting it as it was afterwards.
    /// <summary>The transform the game's sprite batch was begun with.</summary>
    internal static Matrix BatchTransform(SpriteBatch batch) => (Matrix)BatchState[6].GetValue(batch)!;

    internal static void DrawLayer(SpriteBatch batch, Layer layer)
    {
        StepAnimations();
        object?[] state = new object?[BatchState.Length];
        for (int i = 0; i < state.Length; i++)
            state[i] = BatchState[i].GetValue(batch);
        batch.End();
        try
        {
            Renderer renderer = SystemManagers.Default.Renderer;
            renderer.Draw(SystemManagers.Default, layer);
            renderer.ForceEnd();
            renderer.EndFrame();
        }
        finally
        {
            batch.Begin((SpriteSortMode)state[0]!, (BlendState?)state[1], (SamplerState?)state[2], (DepthStencilState?)state[3],
                (RasterizerState?)state[4], (Effect?)state[5], (Matrix)state[6]!);
        }
    }

    // Over an element that takes input (a control, or anything with events), not empty canvas.
    private static bool IsOverGum() =>
        IsInitialized && FrameworkElement.MainCursor.VisualOver is InteractiveGue over && over != GumService.Default.Root && over != OverlayRoot;

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
