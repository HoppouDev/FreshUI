using FreshUI.GumUi;
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

namespace FreshUI.Screens;

/// <summary>
/// The player select (the game's <see cref="UICharacterSelect"/>, which still loads the players when it opens), a
/// <see cref="SelectScreen"/>: a row per player (favourites first, then the last played, as the game's) with the
/// player's portrait (the game's), name with life, mana and difficulty, time played as the aside, and its play,
/// favourite, cloud, rename and delete buttons, calling <see cref="PlayerFileData"/> as the game's rows do.
/// </summary>
internal sealed class PlayerSelectScreen : SelectScreen
{
    public static readonly Func<bool> Shows = GumRoutes.State<UICharacterSelect>();

    public static PlayerSelectScreen? Instance { get; private set; }

    private List<PlayerFileData> _shown = [];

    public PlayerSelectScreen() : base(Language.GetTextValue("UI.SelectPlayer"), GoBack, Language.GetTextValue("UI.New"), () =>
    {
        SoundEngine.PlaySound(10);
        Main.PendingPlayer = new Player();
        Main.menuMode = 888;
        Main.MenuUI.SetState(new UICharacterCreation(Main.PendingPlayer));
    }) => Instance = this;

    private static void GoBack()
    {
        SoundEngine.PlaySound(11);
        Main.menuMode = 0;
    }

    protected override void UpdateRows()
    {
        List<PlayerFileData> players = Main.PlayerList.OrderByDescending(p => p.IsFavorite).ThenByDescending(p => p.LastPlayed)
            .ThenBy(p => p.Name).ThenBy(p => p.GetFileName()).ToList();
        if (players.SequenceEqual(_shown))
            return;
        _shown = players;
        ClearRows();
        foreach (PlayerFileData data in players)
            AddRow(data);
    }

    // A player's row, as the game's UICharacterListItem.
    private void AddRow(PlayerFileData data)
    {
        FreshUI.Components.FreshPanel row = Row();
        var portrait = new UICharacter(data.Player, animated: false, hasBackPanel: true, 1f, useAClone: true);
        row.Children.Add(new FreshUI.Components.FreshGameView
        {
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, area) => DrawPortrait(batch, area, portrait),
        });
        Player player = data.Player;
        Name(row, data.Name, player.loadStatus == StatusID.Ok ? Color.White : Color.Gray);
        (string difficulty, Color color) = player.difficulty switch
        {
            1 => (Language.GetTextValue("UI.Mediumcore"), Main.mcColor),
            2 => (Language.GetTextValue("UI.Hardcore"), Main.hcColor),
            3 => (Language.GetTextValue("UI.Creative"), Main.creativeModeColor),
            _ => (Language.GetTextValue("UI.Softcore"), Color.White),
        };
        Details(row,
            DetailIcon("Images/Heart"), Detail(player.statLifeMax + Language.GetTextValue("GameUI.PlayerLifeMax"), FreshUI.Components.FreshStyle.MutedText),
            DetailIcon("Images/Mana", spaced: true), Detail(player.statManaMax + Language.GetTextValue("GameUI.PlayerManaMax"), FreshUI.Components.FreshStyle.MutedText),
            Detail(difficulty, color, spaced: true));
        TimeSpan played = data.GetPlayTime();
        Aside(row, ((int)played.TotalHours).ToString("D2") + played.ToString("\\:mm\\:ss"));
        float x = ButtonsX;
        Plays.Add(Icon(row, ref x, "Images/UI/ButtonPlay", Language.GetTextValue("UI.Play"), () => Play(data)));
        row.DoubleClick += (_, _) => Play(data);
        Icon(row, ref x, data.IsFavorite ? "Images/UI/ButtonFavoriteActive" : "Images/UI/ButtonFavoriteInactive",
            Language.GetTextValue(data.IsFavorite ? "UI.Unfavorite" : "UI.Favorite"), () =>
            {
                data.ToggleFavorite();
                _shown = [];
            });
        if (SocialAPI.Cloud != null)
        {
            Icon(row, ref x, data.IsCloudSave ? "Images/UI/ButtonCloudActive" : "Images/UI/ButtonCloudInactive",
                Language.GetTextValue(data.IsCloudSave ? "UI.MoveOffCloud" : "UI.MoveToCloud"), () =>
                {
                    if (data.IsCloudSave)
                        data.MoveToLocal();
                    else
                        data.MoveToCloud();
                    _shown = [];
                });
        }
        Icon(row, ref x, "Images/UI/ButtonRename", Language.GetTextValue("UI.Rename"), () => Rename(data));
        if (!data.IsFavorite)
            Delete(row, () => Delete(data));
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

    // The game's part of a row: the player's portrait (on its back panel), at the row's top left.
    private static void DrawPortrait(SpriteBatch batch, Rectangle area, UICharacter portrait)
    {
        Vector2 at = ArtPosition(area);
        portrait.Left.Set(at.X, 0f);
        portrait.Top.Set(at.Y, 0f);
        portrait.Width.Set(ArtSize * 2, 0f);
        portrait.Height.Set(ArtSize * 2, 0f);
        portrait.Recalculate();
        portrait.Draw(batch);
    }
}
