using System.Reflection;
using FreshUI.Components;
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
using Terraria.Map;
using Terraria.Social;
using Terraria.UI;

namespace FreshUI.Screens;

/// <summary>
/// The world select (the game's <see cref="UIWorldSelect"/>, which still loads the worlds when it opens), a
/// <see cref="SelectScreen"/>: a row per world in the game's order (worlds the player can join, a newly generated
/// one, favourites, ones the player has played…) with its icon (the game's, with its badges), name, difficulty, size
/// and creation date, and its play, favourite, cloud, seed, rename and delete buttons. Each row stands for one of the
/// game's own <see cref="UIWorldListItem"/>s, whose handlers its buttons call (the creative mismatch's rejection,
/// multiplayer's next step, the seed copied…).
/// </summary>
internal sealed class WorldSelectScreen : SelectScreen
{
    public static readonly Func<bool> Shows = GumRoutes.State<UIWorldSelect>();
    private static readonly FieldInfo WorldIcon = typeof(UIWorldListItem).GetField("_worldIcon", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo Difficulty = typeof(AWorldListItem).GetMethod("GetDifficulty", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static WorldSelectScreen? Instance { get; private set; }

    private List<WorldFileData> _shown = [];
    private readonly List<(bool Favorite, bool Cloud)> _states = [];

    public WorldSelectScreen() : base(Language.GetTextValue("UI.SelectWorld"), GoBack, Language.GetTextValue("UI.New"), () =>
    {
        SoundEngine.PlaySound(10);
        Main.newWorldName = Lang.gen[57].Value + " " + (Main.WorldList.Count + 1);
        Main.menuMode = 888;
        Main.MenuUI.SetState(new UIWorldCreation());
    }) => Instance = this;

    private static void GoBack()
    {
        SoundEngine.PlaySound(11);
        Main.menuMode = !Main.menuMultiplayer ? 1 : 12;
    }

    private static bool CanJoin(WorldFileData file) => (Main.ActivePlayerFileData.Player.difficulty == 3) == (file.GameMode == 3);

    private static bool IsNewlyGenerated(WorldFileData file) =>
        UIWorldSelect.NewlyGeneratedWorld != null && file.Path == UIWorldSelect.NewlyGeneratedWorld.Path && file.IsCloudSave == UIWorldSelect.NewlyGeneratedWorld.IsCloudSave;

    private static bool HasPlayed(WorldFileData file) => WorldMap.TryGetMapPath(Main.ActivePlayerFileData, file, out _);

    protected override void UpdateRows()
    {
        // As the game's order.
        List<WorldFileData> worlds = Main.WorldList.OrderByDescending(CanJoin).ThenByDescending(IsNewlyGenerated).ThenByDescending(x => x.IsFavorite)
            .ThenByDescending(HasPlayed).ThenByDescending(x => x.LastPlayed).ThenBy(x => x.Name).ThenBy(x => x.GetFileName()).ToList();
        bool changed = !worlds.SequenceEqual(_shown);
        for (int i = 0; !changed && i < worlds.Count; i++)
            changed = _states[i] != (worlds[i].IsFavorite, worlds[i].IsCloudSave);
        if (!changed)
            return;
        _shown = worlds;
        _states.Clear();
        ClearRows();
        int order = 0;
        foreach (WorldFileData data in worlds)
        {
            _states.Add((data.IsFavorite, data.IsCloudSave));
            AddRow(data, new UIWorldListItem(data, order++, CanJoin(data), HasPlayed(data), IsNewlyGenerated(data)));
        }
    }

    // A world's row, standing for the game's list item.
    private void AddRow(WorldFileData data, UIWorldListItem item)
    {
        FreshPanel row = Row();
        var icon = (UIElement)WorldIcon.GetValue(item)!;
        object?[] difficulty = [null, null];
        Difficulty.Invoke(item, difficulty);
        row.Children.Add(new FreshGameView
        {
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, area) => DrawInfo(batch, area, data, icon, (string)difficulty[0]!, (Color)difficulty[1]!),
        });
        string name = data.GetWorldName(allowCropping: true);
        Name(row, data.IsValid ? name : "(" + StatusID.Search.GetName(data.LoadStatus) + ") " + name, data.IsValid ? Color.White : Color.Gray);
        float x = 38;
        Plays.Add(Icon(row, ref x, "Images/UI/ButtonPlay", Language.GetTextValue("UI.Play"), () => Act(item, "PlayGame")));
        Icon(row, ref x, data.IsFavorite ? "Images/UI/ButtonFavoriteActive" : "Images/UI/ButtonFavoriteInactive",
            Language.GetTextValue(data.IsFavorite ? "UI.Unfavorite" : "UI.Favorite"), () => Act(item, "FavoriteButtonClick"));
        if (SocialAPI.Cloud != null)
        {
            Icon(row, ref x, data.IsCloudSave ? "Images/UI/ButtonCloudActive" : "Images/UI/ButtonCloudInactive",
                Language.GetTextValue(data.IsCloudSave ? "UI.MoveOffCloud" : "UI.MoveToCloud"), () => Act(item, "CloudButtonClick"));
        }
        if (data.WorldGeneratorVersion != 0)
            Icon(row, ref x, "Images/UI/ButtonSeed", Language.GetTextValue("UI.CopySeed", data.GetFullSeedText(allowCropping: true)), () => Act(item, "SeedButtonClick"));
        Icon(row, ref x, "Images/UI/ButtonRename", Language.GetTextValue("UI.Rename"), () => Act(item, "RenameButtonClick"));
        if (!data.IsFavorite)
        {
            float right = DeleteX;
            Icon(row, ref right, "Images/UI/ButtonDelete", Language.GetTextValue("UI.Delete"), () => Act(item, "DeleteButtonClick"));
        }
    }

    // One of the game's row handlers, as a click on its button (some set the button's image, so it's given one).
    private static void Act(UIWorldListItem item, string handler)
    {
        var button = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"));
        typeof(UIWorldListItem).GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(item, [new UIMouseEvent(button, Vector2.Zero), button]);
    }

    // The game's part of a row: the world's icon, and its difficulty, size and creation date.
    private static void DrawInfo(SpriteBatch batch, Rectangle area, WorldFileData data, UIElement icon, string difficulty, Color color)
    {
        icon.Left.Set(area.X + 8, 0f);
        icon.Top.Set(area.Y + (area.Height - icon.Height.Pixels) / 2, 0f);
        icon.Recalculate();
        icon.Draw(batch);
        var at = new Vector2(area.X + 300, area.Y + 6);
        Utils.DrawBorderString(batch, Language.GetTextValue("UI.WorldSizeFormat", data.WorldSizeName), at, Color.White);
        var second = new Vector2(area.X + 300, area.Y + 52);
        Utils.DrawBorderString(batch, difficulty, second, color);
        string date = GameCulture.FromCultureName(GameCulture.CultureName.English).IsActive ? data.CreationTime.ToString("d MMMM yyyy") : data.CreationTime.ToShortDateString();
        Utils.DrawBorderString(batch, date, second + new Vector2(130, 0), Color.White);
    }
}
