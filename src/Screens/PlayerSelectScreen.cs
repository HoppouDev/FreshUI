using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Social;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The player select (the game's <see cref="UICharacterSelect"/>, which still loads the players when it opens):
/// a <see cref="FreshWindow"/> with a row per player in a <see cref="FreshScrollViewer"/> (favourites first, then
/// the last played), each with the player's portrait (the game's), name, life and mana, difficulty and time
/// played, and its play, favourite, cloud, rename and delete buttons, as the game's rows; Back and New under it.
/// </summary>
internal sealed class PlayerSelectScreen : ContainerRuntime, IGumScreenUpdate
{
    public static readonly Func<bool> Shows = GumRoutes.State<UICharacterSelect>();
    private const float WindowWidth = 325, RowHeight = 48, Gap = 4, ButtonHeight = 25, Top = 110;

    public static PlayerSelectScreen? Instance { get; private set; }

    private readonly FreshWindow _window = new(Language.GetTextValue("UI.SelectPlayer"));
    private readonly FreshPanel _panel = new();
    private readonly FreshScrollViewer _list = new() { WheelStep = RowHeight + Gap, IsFocusable = false };
    private readonly FreshPanelButton _back, _new;
    private readonly List<FreshIconButton> _plays = [];
    private List<PlayerFileData> _shown = [];

    public PlayerSelectScreen()
    {
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        HasEvents = false;
        InteractiveGue zone = GumGamepad.SpatialZone();
        Children.Add(zone);
        zone.Children.Add(_window);
        _panel.WidthUnits = _panel.HeightUnits = DimensionUnitType.RelativeToParent;
        _panel.Width = 0;
        _window.Add(_panel);
        _list.Visual.X = _list.Visual.Y = 6;
        _list.Visual.WidthUnits = _list.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        _list.Visual.Width = _list.Visual.Height = -12;
        _list.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        _list.InnerPanel.StackSpacing = Gap;
        _panel.Children.Add(_list.Visual);
        _back = Button(Language.GetTextValue("UI.Back"), GoBack);
        _new = Button(Language.GetTextValue("UI.New"), () =>
        {
            SoundEngine.PlaySound(10);
            Main.PendingPlayer = new Player();
            Main.menuMode = 888;
            Main.MenuUI.SetState(new UICharacterCreation(Main.PendingPlayer));
        });
        zone.Children.Add(_back.Visual);
        zone.Children.Add(_new.Visual);
        Instance = this;
    }

    /// <summary>The first player's play button, else New (the gamepad's focus starts there).</summary>
    public FrameworkElement First => _plays.Count > 0 ? _plays[0] : _new;

    private static FreshPanelButton Button(string text, Action onClick)
    {
        var button = new FreshPanelButton { Text = text };
        button.Visual.WidthUnits = button.Visual.HeightUnits = DimensionUnitType.Absolute;
        button.Visual.Height = ButtonHeight;
        button.Click += (_, _) => onClick();
        return button;
    }

    private static void GoBack()
    {
        SoundEngine.PlaySound(11);
        Main.menuMode = 0;
    }

    public void UpdateScreen()
    {
        // As the game's: favourites first, then the last played.
        List<PlayerFileData> players = Main.PlayerList.OrderByDescending(p => p.IsFavorite).ThenByDescending(p => p.LastPlayed)
            .ThenBy(p => p.Name).ThenBy(p => p.GetFileName()).ToList();
        if (!players.SequenceEqual(_shown))
            Build(players);
        float canvasWidth = GraphicalUiElement.CanvasWidth, canvasHeight = GraphicalUiElement.CanvasHeight;
        float height = MathF.Floor(canvasHeight - Top - 20 - ButtonHeight - Gap);
        float x = MathF.Floor((canvasWidth - WindowWidth) / 2);
        if (_window.X != x || _window.Height != height)
        {
            (_window.X, _window.Y, _window.Width, _window.Height) = (x, Top, WindowWidth, height);
            _panel.Height = 0;
            _window.Layout();
            float half = MathF.Floor((WindowWidth - Gap) / 2);
            (_back.Visual.X, _back.Visual.Y, _back.Visual.Width) = (x, Top + height + Gap, half);
            (_new.Visual.X, _new.Visual.Y, _new.Visual.Width) = (x + WindowWidth - half, Top + height + Gap, half);
        }
        // B (the inventory key, the menu's back on the title menu) goes back.
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            GoBack();
        }
    }

    private void Build(List<PlayerFileData> players)
    {
        _shown = players;
        _list.InnerPanel.Children.Clear();
        _plays.Clear();
        foreach (PlayerFileData data in players)
            _list.InnerPanel.Children.Add(Row(data));
    }

    // A player's row, as the game's UICharacterListItem.
    private GraphicalUiElement Row(PlayerFileData data)
    {
        var row = new FreshPanel { WidthUnits = DimensionUnitType.RelativeToParent, Width = 0, Height = RowHeight };
        var portrait = new UICharacter(data.Player, animated: false, hasBackPanel: true, 1f, useAClone: true);
        var info = new FreshGameView
        {
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, area) => DrawInfo(batch, area, data, portrait),
        };
        row.Children.Add(info);
        var name = new TextRuntime
        {
            X = 38,
            Y = 3,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 10,
            Text = data.Name,
            Color = data.Player.loadStatus == StatusID.Ok ? Color.White : Color.Gray,
            HasEvents = false,
        };
        GumText.SetOutline(name, 0.75f, Color.Black);
        row.Children.Add(name);

        float buttonX = 38;
        FreshIconButton play = Icon(row, ref buttonX, "Images/UI/ButtonPlay", Language.GetTextValue("UI.Play"), () => Play(data));
        _plays.Add(play);
        Icon(row, ref buttonX, data.IsFavorite ? "Images/UI/ButtonFavoriteActive" : "Images/UI/ButtonFavoriteInactive",
            Language.GetTextValue(data.IsFavorite ? "UI.Unfavorite" : "UI.Favorite"), () =>
            {
                data.ToggleFavorite();
                _shown = [];
            });
        if (SocialAPI.Cloud != null)
        {
            Icon(row, ref buttonX, data.IsCloudSave ? "Images/UI/ButtonCloudActive" : "Images/UI/ButtonCloudInactive",
                Language.GetTextValue(data.IsCloudSave ? "UI.MoveOffCloud" : "UI.MoveToCloud"), () =>
                {
                    if (data.IsCloudSave)
                        data.MoveToLocal();
                    else
                        data.MoveToCloud();
                    _shown = [];
                });
        }
        Icon(row, ref buttonX, "Images/UI/ButtonRename", Language.GetTextValue("UI.Rename"), () => Rename(data));
        if (!data.IsFavorite)
        {
            float right = WindowWidth - 12 - 6 - 26;
            Icon(row, ref right, "Images/UI/ButtonDelete", Language.GetTextValue("UI.Delete"), () => Delete(data));
        }
        return row;
    }

    // One of a row's image buttons, as FreshUI draws the game's list buttons: just the icon (its button's frame
    // and fill dropped), a texel a UI pixel; its name as its tooltip.
    private static FreshIconButton Icon(FreshPanel row, ref float x, string texture, string name, Action onClick)
    {
        Texture2D art = FreshUI.Legacy.PixelImages.Icon(GumTextures.Load(texture));
        var button = new FreshIconButton { Look = new FreshIconButtonLook(art), TexelSize = 1, HoverColor = FreshStyle.Gold };
        button.Visual.Width = art.Width;
        button.Visual.Height = art.Height;
        button.Visual.X = x;
        button.Visual.Y = RowHeight - 3 - art.Height;
        button.Click += (_, _) => onClick();
        FreshTooltip.Attach(button.Visual, () => name);
        row.Children.Add(button.Visual);
        x += art.Width;
        return button;
    }

    private static void Play(PlayerFileData data)
    {
        if (LoadError(data) is { } error)
        {
            Main.statusText = error;
            Main.menuMode = 15;
            return;
        }
        if (data.Player.loadStatus == StatusID.Ok)
            Main.SelectPlayer(data);
    }

    // Why the player's content didn't load, if it didn't (internal).
    private static readonly Func<PlayerFileData, string?> LoadError = System.Linq.Expressions.Expression.Lambda<Func<PlayerFileData, string?>>(
        System.Linq.Expressions.Expression.Field(PlayerParameter, typeof(PlayerFileData).GetField("ContentLoadError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!),
        PlayerParameter).Compile();
    private static System.Linq.Expressions.ParameterExpression PlayerParameter => _playerParameter ??= System.Linq.Expressions.Expression.Parameter(typeof(PlayerFileData));
    private static System.Linq.Expressions.ParameterExpression? _playerParameter;

    private static void Rename(PlayerFileData data)
    {
        SoundEngine.PlaySound(10);
        Main.clrInput();
        Main.MenuUI.SetState(new UIVirtualKeyboard(Lang.menu[45].Value, data.Name, name =>
        {
            Main.menuMode = 10;
            data.Rename(name.Trim());
            Main.OpenCharacterSelectUI();
        }, Main.OpenCharacterSelectUI, 0, allowEmpty: true));
    }

    private static void Delete(PlayerFileData data)
    {
        int index = Main.PlayerList.IndexOf(data);
        if (index < 0)
            return;
        SoundEngine.PlaySound(10);
        Main.selectedPlayer = index;
        Main.menuMode = 5;
    }

    // The game's part of a row: the portrait, and the life, mana, difficulty and time played on their strips.
    private static void DrawInfo(SpriteBatch batch, Rectangle area, PlayerFileData data, UICharacter portrait)
    {
        portrait.Left.Set(area.X + 8, 0f);
        portrait.Top.Set(area.Y + (area.Height - 58) / 2, 0f);
        portrait.Width.Set(59, 0f);
        portrait.Height.Set(58, 0f);
        portrait.Recalculate();
        portrait.Draw(batch);
        Player player = data.Player;
        // Right of the name and buttons: life and mana on the first line, the difficulty and time played on the second.
        var at = new Vector2(area.X + 300, area.Y + 6);
        string life = player.statLifeMax + Language.GetTextValue("GameUI.PlayerLifeMax"), mana = player.statManaMax + Language.GetTextValue("GameUI.PlayerManaMax");
        batch.Draw(TextureAssets.Heart.Value, at, Color.White);
        Utils.DrawBorderString(batch, life, at + new Vector2(TextureAssets.Heart.Width() + 6, 1), Color.White);
        at.X += 100;
        batch.Draw(TextureAssets.Mana.Value, at, Color.White);
        Utils.DrawBorderString(batch, mana, at + new Vector2(TextureAssets.Mana.Width() + 6, 1), Color.White);
        (string difficulty, Color color) = player.difficulty switch
        {
            1 => (Language.GetTextValue("UI.Mediumcore"), Main.mcColor),
            2 => (Language.GetTextValue("UI.Hardcore"), Main.hcColor),
            3 => (Language.GetTextValue("UI.Creative"), Main.creativeModeColor),
            _ => (Language.GetTextValue("UI.Softcore"), Color.White),
        };
        var second = new Vector2(area.X + 300, area.Y + 52);
        Utils.DrawBorderString(batch, difficulty, second, color);
        TimeSpan played = data.GetPlayTime();
        string time = ((int)played.TotalHours).ToString("D2") + played.ToString("\\:mm\\:ss");
        Utils.DrawBorderString(batch, time, second + new Vector2(130, 0), Color.White);
    }
}
