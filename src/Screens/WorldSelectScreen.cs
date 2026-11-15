using System.Reflection;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
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
/// one, favourites, ones the player has played…): its icon (the game's, with its badges) at the top left, its name
/// beside it with its size and difficulty under that and its creation date at the right, and under the icon its play,
/// favourite, cloud, seed and rename buttons, delete at the line's right end. Each row stands for one of the
/// game's own <see cref="UIWorldListItem"/>s, whose handlers its buttons call (the creative mismatch's rejection,
/// multiplayer's next step, the seed copied…).
/// </summary>
internal sealed class WorldSelectScreen : SelectScreen
{
    public static readonly Func<bool> Shows = GumRoutes.State<UIWorldSelect>();
    private static readonly FieldInfo WorldIcon = typeof(UIWorldListItem).GetField("_worldIcon", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo Difficulty = typeof(AWorldListItem).GetMethod("GetDifficulty", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static WorldSelectScreen? Instance { get; private set; }

    // A row, in UI pixels: the icon (30, the game's 60 units) at its top left, two lines of text beside it, the buttons under it.
    private const float WorldRowHeight = 58, Inset = 6, IconSize = 30, TextX = Inset + IconSize + 6, ButtonsBottom = WorldRowHeight - 4, ArtMargin = 4;

    private List<WorldFileData> _shown = [];
    private readonly List<(bool Favorite, bool Cloud)> _states = [];

    public WorldSelectScreen() : base(Language.GetTextValue("UI.SelectWorld"), GoBack, Language.GetTextValue("UI.New"), () =>
    {
        SoundEngine.PlaySound(10);
        Main.newWorldName = Lang.gen[57].Value + " " + (Main.WorldList.Count + 1);
        Main.menuMode = 888;
        Main.MenuUI.SetState(new UIWorldCreation());
    })
    {
        Instance = this;
        List.WheelStep = WorldRowHeight + Gap;
    }

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
        FreshPanel row = Row(WorldRowHeight);
        var icon = (UIElement)WorldIcon.GetValue(item)!;
        object?[] difficulty = [null, null];
        Difficulty.Invoke(item, difficulty);
        row.Children.Add(new FreshGameView
        {
            WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = 0,
            Draw = (batch, area) => DrawIcon(batch, area, icon),
        });
        string name = data.GetWorldName(allowCropping: true);
        TextRuntime title = Name(row, data.IsValid ? name : "(" + StatusID.Search.GetName(data.LoadStatus) + ") " + name, data.IsValid ? Color.White : Color.Gray);
        (title.X, title.Y) = (TextX, Inset + 1);
        // Under the name: the size, then the difficulty in its colour; the creation date at the right.
        var details = new ContainerRuntime
        {
            X = TextX,
            Y = Inset + 16,
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack,
            StackSpacing = 6,
            HasEvents = false,
        };
        details.Children.Add(Detail(Language.GetTextValue("UI.WorldSizeFormat", data.WorldSizeName), FreshStyle.MutedText));
        details.Children.Add(Detail((string)difficulty[0]!, (Color)difficulty[1]!));
        row.Children.Add(details);
        string created = GameCulture.FromCultureName(GameCulture.CultureName.English).IsActive ? data.CreationTime.ToString("d MMMM yyyy") : data.CreationTime.ToShortDateString();
        TextRuntime date = Detail(created, FreshStyle.MutedText);
        date.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        date.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        (date.X, date.Y) = (-Inset, details.Y);
        row.Children.Add(date);
        // The buttons' art has a few pixels' margin: their glyphs line up with the icon and the date.
        float x = Inset - ArtMargin;
        Plays.Add(Icon(row, ref x, "Images/UI/ButtonPlay", Language.GetTextValue("UI.Play"), () => Act(item, "PlayGame"), ButtonsBottom));
        Icon(row, ref x, data.IsFavorite ? "Images/UI/ButtonFavoriteActive" : "Images/UI/ButtonFavoriteInactive",
            Language.GetTextValue(data.IsFavorite ? "UI.Unfavorite" : "UI.Favorite"), () => Act(item, "FavoriteButtonClick"), ButtonsBottom);
        if (SocialAPI.Cloud != null)
        {
            Icon(row, ref x, data.IsCloudSave ? "Images/UI/ButtonCloudActive" : "Images/UI/ButtonCloudInactive",
                Language.GetTextValue(data.IsCloudSave ? "UI.MoveOffCloud" : "UI.MoveToCloud"), () => Act(item, "CloudButtonClick"), ButtonsBottom);
        }
        if (data.WorldGeneratorVersion != 0)
            Icon(row, ref x, "Images/UI/ButtonSeed", Language.GetTextValue("UI.CopySeed", data.GetFullSeedText(allowCropping: true)), () => Act(item, "SeedButtonClick"), ButtonsBottom);
        Icon(row, ref x, "Images/UI/ButtonRename", Language.GetTextValue("UI.Rename"), () => Act(item, "RenameButtonClick"), ButtonsBottom);
        if (!data.IsFavorite)
        {
            float right = 0;
            FreshIconButton delete = Icon(row, ref right, "Images/UI/ButtonDelete", Language.GetTextValue("UI.Delete"), () => Act(item, "DeleteButtonClick"), ButtonsBottom);
            // At the line's right end, whether or not the list has its scrollbar.
            delete.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            delete.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
            delete.Visual.X = -(Inset - ArtMargin - 1);
        }
    }

    // One of the game's row handlers, as a click on its button (some set the button's image, so it's given one).
    private static void Act(UIWorldListItem item, string handler)
    {
        var button = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"));
        typeof(UIWorldListItem).GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(item, [new UIMouseEvent(button, Vector2.Zero), button]);
    }

    // A detail line's text: smaller than the name, outlined as it.
    private static TextRuntime Detail(string text, Color color)
    {
        var detail = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = 8.5f,
            Text = text,
            Color = color,
            HasEvents = false,
        };
        GumText.SetOutline(detail, 0.75f, Color.Black);
        return detail;
    }

    // The game's part of a row: the world's icon (with its badges), at the row's top left.
    private static void DrawIcon(SpriteBatch batch, Rectangle area, UIElement icon)
    {
        icon.Left.Set(area.X + Inset * 2, 0f);
        icon.Top.Set(area.Y + Inset * 2, 0f);
        icon.Recalculate();
        icon.Draw(batch);
    }
}
