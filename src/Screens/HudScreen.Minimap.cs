using System.Linq.Expressions;
using System.Reflection;
using Aether.UI.Game;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Minimap;

namespace FreshUI.Screens;

/// <summary>
/// The minimap's frame (Aether's <see cref="UIMinimapFrame"/>): the frame over the map and its zoom buttons'
/// hover art are Gum sprites where the game draws them (<c>UIMinimapFrameMixin</c> hands them over in place of
/// <c>DrawForeground</c>). The dark square under the map and the buttons' handling stay the game's, as the map
/// is drawn between them.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Type ButtonType = typeof(MinimapFrame).GetNestedType("Button", BindingFlags.NonPublic)!;
    private static readonly Func<MinimapFrame, Vector2> FramePosition = Member<MinimapFrame, Vector2>(typeof(MinimapFrame), "FramePosition");
    private static readonly Func<MinimapFrame, Asset<Texture2D>> FrameTexture = Member<MinimapFrame, Asset<Texture2D>>(typeof(MinimapFrame), "_frameTexture");
    private static readonly Func<MinimapFrame, object>[] FrameButtons =
        new[] { "_zoomInButton", "_zoomOutButton", "_resetButton" }.Select(name => Member<MinimapFrame, object>(typeof(MinimapFrame), name)).ToArray();
    private static readonly Func<object, Vector2> ButtonPosition = Member<object, Vector2>(ButtonType, "_position");
    private static readonly Func<object, Asset<Texture2D>> ButtonHover = Member<object, Asset<Texture2D>>(ButtonType, "_hoverTexture");
    private static readonly Func<UIMinimapFrame, UIMinimapButton[]> MinimapButtons = Member<UIMinimapFrame, UIMinimapButton[]>(typeof(UIMinimapFrame), "_buttons");

    private static MinimapFrame? _minimapFrame;
    private SpriteRuntime? _frameSprite;
    private readonly SpriteRuntime?[] _frameHovers = new SpriteRuntime?[3];

    /// <summary><c>UIMinimapFrame.DrawForeground</c>: whether Gum draws the frame (the game doesn't then).</summary>
    public static bool TakeMinimapFrame(MinimapFrame frame)
    {
        if (!IsReady)
            return false;
        _minimapFrame = frame;
        return true;
    }

    private void PresentMinimapFrame()
    {
        MinimapFrame? frame = _minimapFrame;
        _minimapFrame = null;
        if (frame == null)
        {
            if (_frameSprite is { Visible: true })
            {
                _frameSprite.Visible = false;
                foreach (SpriteRuntime? hover in _frameHovers)
                    hover!.Visible = false;
            }
            return;
        }
        if (_frameSprite == null)
        {
            _frameSprite = Sprite();
            for (int i = 0; i < 3; i++)
                _frameHovers[i] = Sprite();
        }
        Vector2 at = FramePosition(frame);
        Place(_frameSprite, FrameTexture(frame).Value, at);
        UIMinimapButton[] buttons = MinimapButtons(UIMinimapFrame.Instance);
        for (int i = 0; i < 3; i++)
        {
            object button = FrameButtons[i](frame);
            SpriteRuntime hover = _frameHovers[i]!;
            bool lit = buttons?[i]?.IsHighlighted == true;
            if (hover.Visible != lit)
                hover.Visible = lit;
            if (lit)
                Place(hover, ButtonHover(button).Value, at + ButtonPosition(button));
        }
    }

    private SpriteRuntime Sprite()
    {
        var sprite = new SpriteRuntime { WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
        Children.Insert(0, sprite);
        return sprite;
    }

    // Art at a position in UI units, a texel a unit (half a UI pixel).
    private static void Place(SpriteRuntime sprite, Texture2D texture, Vector2 at)
    {
        if (sprite.Texture != texture)
            (sprite.Texture, sprite.Width, sprite.Height) = (texture, texture.Width / 2f, texture.Height / 2f);
        if (sprite.X != at.X / 2 || sprite.Y != at.Y / 2)
            (sprite.X, sprite.Y) = (at.X / 2, at.Y / 2);
        if (!sprite.Visible)
            sprite.Visible = true;
    }

    // A field or property of a game type that isn't public, as a getter (on an object for a type that isn't either).
    private static Func<TOwner, TValue> Member<TOwner, TValue>(Type type, string name)
    {
        ParameterExpression owner = Expression.Parameter(typeof(TOwner));
        Expression target = typeof(TOwner) == type ? owner : Expression.Convert(owner, type);
        return Expression.Lambda<Func<TOwner, TValue>>(Expression.Convert(Expression.PropertyOrField(target, name), typeof(TValue)), owner).Compile();
    }
}
