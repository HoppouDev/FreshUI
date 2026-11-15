using System.Linq.Expressions;
using System.Reflection;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.Initializers;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The on-screen keyboard (the game's <see cref="UIVirtualKeyboard"/>, on the title menu and as a fancy UI in game:
/// renames, the world's name and seed, servers and passwords, signs and chests) in FreshUI's components, by
/// <see cref="GameUiMirror"/>: its keys, shift and symbols, space, backspace, Cancel and Submit, its label and text
/// box (the text with the caret at its end). The game's keyboard keeps its state and handlers; its own per-frame
/// work (typing, Enter and Esc, the sign or chest it edits, its layout with or without the keys) runs from here,
/// as the game ran it while drawing. On the gamepad its shortcuts are the game's (symbols, backspace, space and
/// submit, in the instruction bar), and B cancels.
/// </summary>
internal sealed class VirtualKeyboardScreen : ContainerRuntime, IGumScreenUpdate
{
    public static readonly Func<bool> Shows = GumRoutes.State<UIVirtualKeyboard>();

    // The keyboard's own per-frame work is in its DrawSelf (which draws nothing of its own: its children draw).
    private static readonly Action<UIVirtualKeyboard, SpriteBatch> DrawSelf = Compile();

    public static VirtualKeyboardScreen? Instance { get; private set; }

    private readonly GameUiMirror _mirror = new();

    public VirtualKeyboardScreen()
    {
        Instance = this;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        Children.Add(new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, _) =>
            {
                if (_mirror.Source is UIVirtualKeyboard keyboard)
                    DrawSelf(keyboard, batch);
            },
        });
        Children.Add(_mirror);
    }

    public Gum.Forms.Controls.FrameworkElement? First => _mirror.First;

    /// <summary>The instruction bar's hints, which also take the game's keyboard shortcuts (as the game's fancy UI page does).</summary>
    public static string Hints() => UILinksInitializer.FancyUISpecialInstructions();

    public void UpdateScreen()
    {
        _mirror.Source = Main.MenuUI.CurrentState as UIVirtualKeyboard ?? (Main.inFancyUI ? Main.InGameUI.CurrentState as UIVirtualKeyboard : null);
        _mirror.Update();
        // B, as the game's back command for the keyboard.
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            UIVirtualKeyboard.Cancel();
        }
    }

    private static Action<UIVirtualKeyboard, SpriteBatch> Compile()
    {
        ParameterExpression keyboard = Expression.Parameter(typeof(UIVirtualKeyboard)), batch = Expression.Parameter(typeof(SpriteBatch));
        MethodInfo method = typeof(UIVirtualKeyboard).GetMethod("DrawSelf", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return Expression.Lambda<Action<UIVirtualKeyboard, SpriteBatch>>(Expression.Call(keyboard, method, batch), keyboard, batch).Compile();
    }
}
