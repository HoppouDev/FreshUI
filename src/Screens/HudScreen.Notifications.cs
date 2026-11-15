using System.Linq.Expressions;
using Aether.UI.Game;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Achievements;
using Terraria.GameInput;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The in-game notifications stacked upwards from above the hotbar (Aether's <see cref="UINotifications"/>):
/// "achievement unlocked" as a <see cref="FreshPanel"/> with the achievement's name and icon, brighter while
/// hovered, a click opening it in the achievements list; any other kind (a friend asking to join…) drawn by
/// its own code in a game view, as the game's. Each fades as the game's does.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> NotificationsAsked = GumRoutes.GameScreen<UINotifications>();
    private static readonly Func<List<IInGameNotification>> Notifications = Expression.Lambda<Func<List<IInGameNotification>>>(
        Expression.Field(null, typeof(InGameNotificationsTracker).GetField("_notifications", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!)).Compile();
    private static readonly Type PopupType = typeof(InGamePopups.AchievementUnlockedPopup);
    private static readonly Func<object, string> PopupTitle = Member<object, string>(PopupType, "_title");
    private static readonly Func<object, float> PopupOpacity = Member<object, float>(PopupType, "Opacity");
    private static readonly Func<object, Asset<Texture2D>> PopupTexture = Member<object, Asset<Texture2D>>(PopupType, "_achievementTexture");
    private static readonly Func<object, Rectangle> PopupFrame = Member<object, Rectangle>(PopupType, "_achievementIconFrame");
    private static readonly Func<object, Achievement> PopupAchievement = Member<object, Achievement>(PopupType, "_theAchievement");
    private static readonly Action<object> DismissPopup = DismissAction();
    private static readonly Color PopupColor = new(64, 109, 164);

    private readonly Dictionary<IInGameNotification, (FreshPanel Panel, SpriteRuntime Icon, TextRuntime Title)> _popups = [];
    private readonly List<(IInGameNotification Notification, Vector2 Anchor)> _otherNotifications = [];
    private readonly HashSet<IInGameNotification> _currentNotifications = [];
    private readonly List<IInGameNotification> _goneNotifications = [];
    private FreshGameView? _notificationView;

    private void PresentNotifications()
    {
        _otherNotifications.Clear();
        _currentNotifications.Clear();
        if (IsReady && NotificationsAsked())
        {
            // The screen's bottom centre in UI units (the canvas is half as many UI pixels), 40 up (65 with the gamepad's hints).
            var anchor = new Vector2(Gum.Wireframe.GraphicalUiElement.CanvasWidth, Gum.Wireframe.GraphicalUiElement.CanvasHeight * 2 - 40 - (PlayerInput.UsingGamepad ? 25 : 0));
            // Above FreshUI's hotbar (at the bottom, where the game had none) and its held item's name.
            if (TrySlotPosition(13, 0, out Vector2 hotbar))
                anchor.Y = MathF.Min(anchor.Y, hotbar.Y - 30);
            foreach (IInGameNotification notification in Notifications())
            {
                _currentNotifications.Add(notification);
                if (notification is InGamePopups.AchievementUnlockedPopup)
                    PresentAchievementPopup(notification, anchor);
                else
                    _otherNotifications.Add((notification, anchor));
                notification.PushAnchor(ref anchor);
                if (anchor.Y < -100f)
                    break;
            }
        }
        _goneNotifications.Clear();
        foreach ((IInGameNotification notification, var popup) in _popups)
        {
            if (!_currentNotifications.Contains(notification))
            {
                Children.Remove(popup.Panel);
                _goneNotifications.Add(notification);
            }
        }
        foreach (IInGameNotification gone in _goneNotifications)
            _popups.Remove(gone);
        if (_otherNotifications.Count > 0 && _notificationView == null)
        {
            _notificationView = new FreshGameView
            {
                WidthUnits = DimensionUnitType.RelativeToParent,
                HeightUnits = DimensionUnitType.RelativeToParent,
                Width = 0,
                Height = 0,
                Draw = (batch, _) =>
                {
                    foreach ((IInGameNotification notification, Vector2 at) in _otherNotifications)
                        notification.DrawInGame(batch, at);
                },
            };
            Children.Insert(0, _notificationView);
        }
    }

    // An achievement's popup, its bottom centre at the anchor (UI units).
    private void PresentAchievementPopup(IInGameNotification notification, Vector2 anchor)
    {
        if (!_popups.TryGetValue(notification, out var popup))
        {
            var panel = new FreshPanel { Height = 18 };
            var icon = new SpriteRuntime
            {
                TextureAddress = Gum.Managers.TextureAddress.Custom,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = 14,
                Height = 14,
                Y = 2,
            };
            Texture2D texture = PopupTexture(notification).Value;
            Rectangle frame = PopupFrame(notification);
            (icon.Texture, icon.TextureLeft, icon.TextureTop, icon.TextureWidth, icon.TextureHeight) = (texture, frame.X, frame.Y, frame.Width, frame.Height);
            var title = new TextRuntime
            {
                WidthUnits = DimensionUnitType.RelativeToChildren,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                FontSize = 9,
                X = 6,
                Y = 4,
                Text = PopupTitle(notification),
                HasEvents = false,
            };
            GumText.SetOutline(title, 0.5f, Color.Black);
            panel.Children.Add(title);
            panel.Children.Add(icon);
            panel.Width = MathF.Ceiling(title.GetAbsoluteWidth()) + 6 + 4 + icon.Width + 4;
            icon.X = panel.Width - 4 - icon.Width;
            Children.Insert(0, panel);
            popup = _popups[notification] = (panel, icon, title);
        }
        float opacity = PopupOpacity(notification);
        bool shown = opacity > 0f;
        if (popup.Panel.Visible != shown)
            popup.Panel.Visible = shown;
        if (!shown)
            return;
        float x = MathF.Round((anchor.X - popup.Panel.Width) / 2), y = MathF.Round(anchor.Y / 2 - popup.Panel.Height);
        if (popup.Panel.X != x || popup.Panel.Y != y)
            (popup.Panel.X, popup.Panel.Y) = (x, y);
        bool hovered = HudPoints.Hovered(popup.Panel);
        Tint(popup.Panel, PopupColor * (hovered ? 0.75f : 0.5f), opacity);
        if (popup.Icon.Color != Color.White * opacity)
            popup.Icon.Color = Color.White * opacity;
        byte c = Main.mouseTextColor;
        SetColor(popup.Title, new Color(c, c, c / 5, c) * opacity);
        if (hovered)
        {
            Main.LocalPlayer.mouseInterface = true;
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                IngameFancyUI.OpenAchievementsAndGoto(PopupAchievement(notification));
                DismissPopup(notification);
            }
        }
    }

    // As the game's popup when clicked: its time up, to be removed.
    private static Action<object> DismissAction()
    {
        ParameterExpression popup = Expression.Parameter(typeof(object));
        Expression typed = Expression.Convert(popup, PopupType);
        return Expression.Lambda<Action<object>>(Expression.Block(
            Expression.Assign(Expression.PropertyOrField(typed, "_ingameDisplayTimeLeft"), Expression.Constant(0)),
            Expression.Assign(Expression.Property(typed, "ShouldBeRemoved"), Expression.Constant(true))), popup).Compile();
    }
}
