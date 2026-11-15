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
/// player's portrait (the game's), name, life and mana, difficulty and time played, and its play, favourite, cloud,
/// rename and delete buttons, calling <see cref="PlayerFileData"/> as the game's rows do.
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
            Draw = (batch, area) => DrawInfo(batch, area, data, portrait),
        });
        Name(row, data.Name, data.Player.loadStatus == StatusID.Ok ? Color.White : Color.Gray);
        float x = 38;
        Plays.Add(Icon(row, ref x, "Images/UI/ButtonPlay", Language.GetTextValue("UI.Play"), () => Play(data)));
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
        {
            float right = DeleteX;
            Icon(row, ref right, "Images/UI/ButtonDelete", Language.GetTextValue("UI.Delete"), () => Delete(data));
        }
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
