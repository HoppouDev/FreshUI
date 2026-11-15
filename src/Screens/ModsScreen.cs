using System.Buffers.Binary;
using System.Diagnostics;
using Aether.Modding;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// Aether's Mods window (its internal <c>UIModList</c>, on the title menu and, from the pause menu, as a fancy UI
/// in game), in the settings window's layout: a "Mods" title tab over two panels. The left one has a search field,
/// the loaded mods as rows (icon, name, ID and version; the chosen one lit) and how many show, with Open Mods
/// Folder and Back at its bottom; the right one has the chosen mod's details (name, ID and version, authors,
/// description, contributors, licence) and its Homepage and Source links. As Aether's: rows sorted by name, the
/// search matching names, IDs and authors, the first match chosen when the chosen one is filtered out; Back (or
/// Esc, or B) goes to the main menu, or in game closes the window.
/// </summary>
internal sealed class ModsScreen : ContainerRuntime, IGumScreenUpdate
{
    // In UI pixels, as the settings window's.
    private const float WindowWidth = 480, GameHeight = 320, GameMargin = 24, MenuTop = 109, MenuMargin = 22;
    private const float Gap = 5, Padding = 6, LeftShare = 0.4f, ActionHeight = 25, LinkHeight = 16;
    private const float RowHeight = 29, IconSize = 20, SearchHeight = 16, CountHeight = 14;

    private static readonly Type? ModListType = typeof(Main).Assembly.GetType("Aether.UI.Menu.UIModList");
    private static bool Disabled => Program.LaunchParameters.ContainsKey("-nomods");

    static ModsScreen() => GumRoutes.StateKind(state => state.GetType() == ModListType);

    /// <summary>The game's Mods window, if it's open (the title menu's state, or the fancy UI's in game).</summary>
    private static UIState? Current => Main.MenuUI?.CurrentState is { } menu && menu.GetType() == ModListType ? menu
        : Main.inFancyUI && Main.InGameUI?.CurrentState is { } fancy && fancy.GetType() == ModListType ? fancy : null;

    public static bool Shows => Current != null;

    public static ModsScreen? Instance { get; private set; }

    private readonly RectangleRuntime _backdrop = FreshStyle.Backdrop(FreshStyle.Dim);
    private readonly FreshWindow _window = new(Language.GetTextValue("AetherMods.Title"));
    private readonly FreshPanel _browser = new(), _detailPanel = new();
    private readonly FreshTextBox _search = new() { Placeholder = Language.GetTextValue("AetherMods.Search") };
    private readonly FreshScrollViewer _list = Scroller(RowHeight + 2), _details = Scroller(16);
    private readonly TextRuntime _count = Text(8, FreshStyle.MutedText);
    private readonly FreshPanelButton _folder, _back, _homepage, _source;
    private readonly List<(ModInfo Mod, FreshPanelButton Row)> _rows = [];
    private readonly List<Texture2D> _icons = [];
    private UIState? _shownFor;
    private ModInfo? _selected;
    private string _query = "";

    public ModsScreen()
    {
        Instance = this;
        WidthUnits = HeightUnits = DimensionUnitType.RelativeToParent;
        Width = Height = 0;
        Children.Add(_backdrop);
        InteractiveGue zone = GumGamepad.SpatialZone();
        Children.Add(zone);
        zone.Children.Add(_window);
        _window.Add(_browser);
        _window.Add(_detailPanel);

        _search.Visual.X = _search.Visual.Y = Padding;
        _search.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        _search.Visual.Width = -2 * Padding;
        _search.Visual.Height = SearchHeight;
        _search.TextChanged += (_, _) => Filter(_search.Text ?? "");
        _browser.Children.Add(_search.Visual);
        _list.Visual.X = Padding;
        _list.Visual.Y = Padding + SearchHeight + Gap;
        _browser.Children.Add(_list.Visual);
        _count.X = Padding + 2;
        _browser.Children.Add(_count);
        _folder = Button(Language.GetTextValue("AetherMods.OpenFolder"), 12, OpenModsFolder);
        _back = Button(Language.GetTextValue("UI.Back"), 12, Back);
        _browser.Children.Add(_folder.Visual);
        _browser.Children.Add(_back.Visual);

        _details.Visual.X = _details.Visual.Y = Padding;
        _detailPanel.Children.Add(_details.Visual);
        _homepage = Button(Language.GetTextValue("AetherMods.Homepage"), 10, () => OpenLink(_selected?.Homepage));
        _source = Button(Language.GetTextValue("AetherMods.Source"), 10, () => OpenLink(_selected?.Source));
        _detailPanel.Children.Add(_homepage.Visual);
        _detailPanel.Children.Add(_source.Visual);
    }

    /// <summary>The gamepad's starting point: the chosen mod's row, else the first, else Back.</summary>
    public FrameworkElement First => _rows.Find(r => r.Mod == _selected).Row ?? (_rows.Count > 0 ? _rows[0].Row : _back);

    private static FreshScrollViewer Scroller(float step)
    {
        var scroller = new FreshScrollViewer { WheelStep = step, IsFocusable = false };
        scroller.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        scroller.InnerPanel.StackSpacing = 2;
        return scroller;
    }

    private static TextRuntime Text(float size, Color color, bool wrap = false)
    {
        var text = new TextRuntime
        {
            WidthUnits = wrap ? DimensionUnitType.RelativeToParent : DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = size,
            Color = color,
            HasEvents = false,
        };
        GumText.SetOutline(text, 0.75f, Color.Black);
        return text;
    }

    private static FreshPanelButton Button(string text, float fontSize, Action onClick)
    {
        var button = new FreshPanelButton { Text = text, FontSize = fontSize };
        button.Visual.WidthUnits = button.Visual.HeightUnits = DimensionUnitType.Absolute;
        button.Click += (_, _) => onClick();
        return button;
    }

    public void UpdateScreen()
    {
        UIState? state = Current;
        if (state == null)
            return;
        if (state != _shownFor)
            Open(state);
        bool inGame = !Main.gameMenu;
        // Modal in game (the game takes no clicks while it shows), over the game dimmed.
        if (_backdrop.Visible != inGame)
            _backdrop.Visible = HasEvents = inGame;
        Layout(inGame);
        // Esc goes back unless it's ending the search's typing; B (the inventory key) on the title menu.
        if (_search.IsFocused)
        {
            if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
                _search.IsFocused = false;
        }
        else if (Main.gameMenu && !PlayerInput.UsingGamepad && Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
            Back();
        if (UILinkPointNavigator.Shortcuts.BackButtonInUse && !UILinkPointNavigator.Shortcuts.BackButtonLock && Main.gameMenu)
        {
            UILinkPointNavigator.Shortcuts.BackButtonLock = true;
            Back();
        }
    }

    // The window just opened: the mods as Aether lists them, the search cleared.
    private void Open(UIState state)
    {
        _shownFor = state;
        foreach (Texture2D icon in _icons)
            icon.Dispose();
        _icons.Clear();
        _rows.Clear();
        Texture2D fallback = GumTextures.Load("Images/UI/DefaultResourcePackIcon");
        foreach (ModInfo mod in ModCatalog.LoadedMods.OrderBy(mod => mod.Name, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(mod => mod.Id, StringComparer.Ordinal))
            _rows.Add((mod, Row(mod, LoadIcon(mod) ?? fallback)));
        _selected = null;
        if (_search.Text != "")
            _search.Text = "";
        Filter("", force: true);
    }

    // A mod's row: its icon, its name, and its ID and version under it, lit while chosen.
    private FreshPanelButton Row(ModInfo mod, Texture2D icon)
    {
        var row = new FreshPanelButton { Text = "" };
        row.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        row.Visual.Width = 0;
        row.Visual.HeightUnits = DimensionUnitType.Absolute;
        row.Visual.Height = RowHeight;
        row.Visual.Name = mod.Name;
        row.Visual.Children.Add(new SpriteRuntime
        {
            Texture = icon,
            X = 4,
            Y = (RowHeight - IconSize) / 2,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = IconSize,
            Height = IconSize,
        });
        TextRuntime name = Text(9.5f, Color.White), identity = Text(7.5f, FreshStyle.MutedText);
        name.Text = Line(mod.Name);
        identity.Text = Line(Language.GetTextValue("AetherMods.Identity", mod.Id, mod.Version));
        (name.X, name.Y, identity.X, identity.Y) = (IconSize + 9, 4, IconSize + 9, 16);
        row.Visual.Children.Add(name);
        row.Visual.Children.Add(identity);
        row.Click += (_, _) =>
        {
            SoundEngine.PlaySound(12);
            Select(mod);
        };
        return row;
    }

    // Manifest text is shown as written, on one line (as Aether's).
    private static string Line(string text) => Utils.TrimUserString(text.Replace('\r', ' ').Replace('\n', ' '), 200);

    private static bool Matches(ModInfo mod, string query) => query.Length == 0 ||
        mod.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
        mod.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        mod.Authors.Any(author => author.Contains(query, StringComparison.CurrentCultureIgnoreCase));

    private void Filter(string text, bool force = false)
    {
        string query = text.Trim();
        if (query == _query && !force)
            return;
        _query = query;
        _list.InnerPanel.Children.Clear();
        _list.VerticalScrollBarValue = 0;
        ModInfo? first = null;
        bool hasSelection = false;
        int count = 0;
        foreach ((ModInfo mod, FreshPanelButton row) in _rows)
        {
            if (!Matches(mod, query))
                continue;
            _list.InnerPanel.Children.Add(row.Visual);
            first ??= mod;
            hasSelection |= mod == _selected;
            count++;
        }
        if (count == 0)
        {
            TextRuntime empty = Text(8.5f, FreshStyle.MutedText, wrap: true);
            empty.Text = Language.GetTextValue(Disabled ? "AetherMods.Disabled" : _rows.Count == 0 ? "AetherMods.Empty" : "AetherMods.NoMatches");
            _list.InnerPanel.Children.Add(empty);
        }
        _count.Text = query.Length == 0 ? Language.GetTextValue("AetherMods.Loaded", _rows.Count)
            : Language.GetTextValue("AetherMods.Filtered", count, _rows.Count);
        Select(hasSelection ? _selected : first);
    }

    private void Select(ModInfo? mod)
    {
        _selected = mod;
        foreach ((ModInfo rowMod, FreshPanelButton row) in _rows)
            row.IsChosen = rowMod == mod;
        _details.InnerPanel.Children.Clear();
        _details.VerticalScrollBarValue = 0;
        if (mod != null)
        {
            Detail(mod.Name, 12, Color.White);
            Detail(Language.GetTextValue("AetherMods.Identity", mod.Id, mod.Version), 8, FreshStyle.MutedText);
            Metadata("AetherMods.Authors", mod.Authors);
            if (!string.IsNullOrWhiteSpace(mod.Description))
                Detail(mod.Description, 9.5f, Color.White);
            Metadata("AetherMods.Contributors", mod.Contributors);
            Metadata("AetherMods.License", mod.Licenses);
        }
        else if (_rows.Count == 0)
            Detail(Language.GetTextValue(Disabled ? "AetherMods.RestartWithMods" : "AetherMods.AddMods"), 9.5f, FreshStyle.MutedText);
        _homepage.Visual.Visible = Link(mod?.Homepage) != null;
        _source.Visual.Visible = Link(mod?.Source) != null;
    }

    private void Detail(string text, float size, Color color)
    {
        TextRuntime detail = Text(size, color, wrap: true);
        detail.Text = Utils.TrimUserString(text.Replace("\r", ""), 8192);
        _details.InnerPanel.Children.Add(detail);
    }

    private void Metadata(string key, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
            Detail(Language.GetTextValue(key, string.Join(", ", values)), 8.5f, FreshStyle.MutedText);
    }

    private void Layout(bool inGame)
    {
        float canvasWidth = GraphicalUiElement.CanvasWidth, canvasHeight = GraphicalUiElement.CanvasHeight;
        float width = MathF.Floor(MathF.Min(WindowWidth, canvasWidth * 0.88f));
        float height, top;
        if (inGame)
        {
            height = MathF.Floor(MathF.Min(GameHeight, canvasHeight - GameMargin * 2));
            top = MathF.Floor((canvasHeight - height) / 2 + GameMargin / 4);
        }
        else
        {
            top = MenuTop;
            height = MathF.Floor(canvasHeight - MenuTop - MenuMargin);
        }
        float x = MathF.Floor((canvasWidth - width) / 2);
        if (_window.X == x && _window.Y == top && _window.Width == width && _window.Height == height && _homepage.Visual.Visible == _homepageShown && _source.Visual.Visible == _sourceShown)
            return;
        (_window.X, _window.Y, _window.Width, _window.Height) = (x, top, width, height);
        (_homepageShown, _sourceShown) = (_homepage.Visual.Visible, _source.Visual.Visible);
        _window.Layout();
        float left = MathF.Floor(width * LeftShare), inner = left - 2 * Padding;
        (_browser.X, _browser.Width, _browser.Height) = (0, left, height);
        float right = width - left - Gap;
        (_detailPanel.X, _detailPanel.Width, _detailPanel.Height) = (left + Gap, right, height);
        // The left panel: the search, the rows, the count, then Open Mods Folder and Back at its bottom.
        float backY = height - Padding - ActionHeight, folderY = backY - Gap - ActionHeight, countY = folderY - Gap - CountHeight;
        foreach ((FreshPanelButton button, float y) in new[] { (_folder, folderY), (_back, backY) })
            (button.Visual.X, button.Visual.Y, button.Visual.Width, button.Visual.Height) = (Padding, y, inner, ActionHeight);
        _count.Y = countY + 2;
        (_list.Visual.Width, _list.Visual.Height) = (inner, countY - Gap - _list.Visual.Y);
        // The right panel: the details, and the links under them, side by side.
        bool links = _homepageShown || _sourceShown;
        float innerRight = right - 2 * Padding, half = MathF.Floor((innerRight - Gap) / 2), linkY = height - Padding - LinkHeight;
        (_details.Visual.Width, _details.Visual.Height) = (innerRight, (links ? linkY - Gap : height - Padding) - Padding);
        (_homepage.Visual.X, _homepage.Visual.Y, _homepage.Visual.Width, _homepage.Visual.Height) = (Padding, linkY, half, LinkHeight);
        float sourceX = _homepageShown ? Padding + innerRight - half : Padding;
        (_source.Visual.X, _source.Visual.Y, _source.Visual.Width, _source.Visual.Height) = (sourceX, linkY, half, LinkHeight);
    }

    private bool _homepageShown, _sourceShown;

    private static void Back()
    {
        if (!Main.gameMenu)
        {
            IngameFancyUI.Close();
            return;
        }
        SoundEngine.PlaySound(11);
        Main.menuMode = 0;
    }

    // A web link from the manifest, as Aether accepts them: absolute http or https, with a host.
    private static string? Link(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) && uri.Host.Length > 0
            ? uri.AbsoluteUri : null;

    private static void OpenLink(string? value)
    {
        if (Link(value) is { } uri)
            OpenTarget(uri);
    }

    private static void OpenModsFolder()
    {
        string folder = Path.GetFullPath(Path.Combine(Program.SavePath, "Mods"));
        if (Utils.TryCreatingDirectory(folder))
            OpenTarget(folder);
    }

    private static void OpenTarget(string target)
    {
        SoundEngine.PlaySound(12);
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"FreshUI: could not open '{target}': {exception.Message}");
        }
    }

    // The mod's icon, as Aether reads it: a PNG up to 1024 pixels a side (and 4 MiB), scaled to 64.
    private Texture2D? LoadIcon(ModInfo mod)
    {
        if (mod.IconPath == null)
            return null;
        try
        {
            using Stream stream = ModResources.OpenIcon(mod.Id);
            if (stream.Length is < 24 or > 4 * 1024 * 1024)
                return null;
            Span<byte> header = stackalloc byte[24];
            stream.ReadExactly(header);
            if (BinaryPrimitives.ReadUInt64BigEndian(header) != 0x89504E470D0A1A0AUL ||
                !header.Slice(12, 4).SequenceEqual("IHDR"u8) ||
                BinaryPrimitives.ReadUInt32BigEndian(header[16..]) is 0 or > 1024 ||
                BinaryPrimitives.ReadUInt32BigEndian(header[20..]) is 0 or > 1024)
                return null;
            stream.Position = 0;
            var icon = Texture2D.FromStream(Main.instance.GraphicsDevice, stream, 64, 64, zoom: false);
            // A picture, not pixel art: filtered as Aether drew it, not point-sampled as the UI's art.
            icon.SamplerEXT = SamplerState.LinearClamp;
            _icons.Add(icon);
            return icon;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FreshUI: could not read the icon for '{mod.Id}': {exception.Message}");
            return null;
        }
    }
}
