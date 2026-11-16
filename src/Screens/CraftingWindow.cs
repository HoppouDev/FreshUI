using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using FreshUI.Components;
using FreshUI.GumUi;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The crafting window (the game's <see cref="NewCraftingUI"/>, its modern grid), in FreshUI's style: the item
/// category filters as tabs over a panel with the window's title (or its crafting station's) and a search box,
/// the chosen recipe with its ingredients and how many of each the player has (with the Guide, the item to
/// look up and what it needs), and every recipe in a scrolling grid, those the player can't make yet dimmed.
/// The window's state stays the game's (its recipes, filters, search and chosen recipe, through its private
/// members): it's only no longer drawn or updated (<see cref="GumRoutes.State{T}"/>). Hovering and clicking a
/// recipe or ingredient is the game's, at the game's cursor (the mouse, or the gamepad's on the window's
/// points, which are placed on the Gum controls); the tabs and the search box are Gum buttons.
/// </summary>
internal sealed class CraftingWindow : ContainerRuntime
{
    private const float Left = 10, Top = 156, Width_ = 245, TabSize = 20, TabGap = 2, TabsHeight = 19;
    private const float Pad = 6, CraftTop = 22, CraftScale = FreshItemSlot.DefaultScale, MaterialScale = 0.6f, GridScale = FreshItemSlot.DefaultScale, GridGap = 2;
    private static Color Disabled => new(160, 160, 160, 255);
    private static Color Enough => new(144, 238, 144, 255);
    private static Color NotEnough => new(255, 140, 160, 255);

    static CraftingWindow() => GumRoutes.State<NewCraftingUI>();

    /// <summary>Whether the window shows (the game would draw it).</summary>
    public static bool Shows => NewCraftingUI.Visible && !Main.inFancyUI && Main.playerInventory;

    private readonly FreshPanel _panel;
    private readonly TextRuntime _title, _guideText, _requirements;
    private readonly FreshPanelButton _search;
    private readonly List<FreshPanelButton> _tabs = [];
    private readonly FreshItemSlot _craft, _guide;
    private readonly SpriteRuntime _arrow;
    private readonly FreshItemSlot[] _materials = new FreshItemSlot[Recipe.maxRequirements];
    private readonly TextRuntime[] _owned = new TextRuntime[Recipe.maxRequirements];
    private readonly int[] _ownedShown = Enumerable.Repeat(-1, Recipe.maxRequirements).ToArray();
    private readonly FreshSeparator _separator;
    private readonly FreshScrollViewer _grid;
    private readonly List<FreshItemSlot> _gridSlots = [];
    private readonly Item[] _guideItem = new Item[1];
    private int _tabCount = -1;

    public CraftingWindow()
    {
        X = Left;
        Y = Top;
        Width = Width_;
        HeightUnits = DimensionUnitType.RelativeToParent;
        Height = -Top - TabsHeight;
        HasEvents = false;
        Visible = false;

        _panel = new FreshPanel
        {
            Y = TabsHeight,
            WidthUnits = DimensionUnitType.RelativeToParent,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Width = 0,
            Height = -TabsHeight,
        };
        Children.Add(_panel);
        _title = Text(10.5f, Color.White);
        _title.X = Pad;
        _title.Y = 4;
        _panel.Children.Add(_title);
        _search = new FreshPanelButton { FontSize = 9 };
        _search.AlignTextLeft(4);
        _search.Visual.Y = 3;
        _search.Visual.WidthUnits = DimensionUnitType.Absolute;
        _search.Visual.HeightUnits = DimensionUnitType.Absolute;
        _search.Visual.Height = 15;
        _search.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        _search.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        _search.Visual.X = -Pad;
        _search.Click += (_, _) => ToggleSearch();
        _panel.Children.Add(_search.Visual);

        _craft = Slot(CraftScale);
        _craft.Visual.X = Pad;
        _craft.Visual.Y = CraftTop;
        _panel.Children.Add(_craft.Visual);
        Texture2D arrow = GumTextures.Load("Images/UI/Craft");
        _arrow = new SpriteRuntime
        {
            Texture = arrow,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Width = MathF.Round(arrow.Width * FreshStyle.Texel),
            Height = MathF.Round(arrow.Height * FreshStyle.Texel),
            X = Pad + _craft.Size + 3,
        };
        _arrow.Y = CraftTop + MathF.Round((_craft.Size - _arrow.Height) / 2);
        _panel.Children.Add(_arrow);
        for (int i = 0; i < _materials.Length; i++)
        {
            _materials[i] = Slot(MaterialScale);
            _materials[i].Visual.Visible = false;
            _panel.Children.Add(_materials[i].Visual);
            _owned[i] = Text(7, Enough);
            _owned[i].Visible = false;
            _panel.Children.Add(_owned[i]);
        }
        _requirements = Text(9, Color.White);
        _requirements.X = Pad;
        _panel.Children.Add(_requirements);
        _guide = Slot(CraftScale);
        _guide.Visual.X = Pad;
        _panel.Children.Add(_guide.Visual);
        _guideText = Text(9, Color.White);
        _guideText.X = Pad + _guide.Size + 4;
        _panel.Children.Add(_guideText);

        _separator = new FreshSeparator { X = Pad, Width = -2 * Pad };
        _panel.Children.Add(_separator);
        _grid = new FreshScrollViewer { WheelStep = FreshItemSlot.SizeAt(GridScale) + GridGap };
        _grid.Visual.X = Pad;
        _grid.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        _grid.Visual.Width = -Pad - 3;
        _grid.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
        _grid.InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        _grid.InnerPanel.WrapsChildren = true;
        _grid.InnerPanel.StackSpacing = GridGap;
        _panel.Children.Add(_grid.Visual);
    }

    private static TextRuntime Text(float size, Color color)
    {
        var text = new TextRuntime
        {
            WidthUnits = DimensionUnitType.RelativeToChildren,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Width = 0,
            Height = 0,
            FontSize = size,
            Color = color,
            HasEvents = false,
        };
        GumText.SetOutline(text, 0.5f, Color.Black);
        return text;
    }

    // The game's cursor handles the slots (as the HUD's), so Gum's doesn't.
    private static FreshItemSlot Slot(float scale)
    {
        var slot = new FreshItemSlot { Scale = scale };
        slot.Visual.HasEvents = false;
        return slot;
    }

    private bool _wasShown;
    private string _searchText = "";
    private bool _writing;
    private float _searchPointX;
    private object?[] _shownEntries = [];
    private int _shownCount;
    private readonly bool[] _available = new bool[Recipe.maxRecipes];
    private readonly int[] _availableIndex = new int[Recipe.maxRecipes];

    /// <summary>Each frame, at draw time (the game's cursor in UI units, as where the game drew the window).</summary>
    public void Present()
    {
        NewCraftingUI? ui = Instance();
        bool shown = Shows && ui != null;
        if (Visible != shown)
            Visible = shown;
        if (!shown)
        {
            if (_wasShown)
                StopSearch();
            _wasShown = false;
            return;
        }
        if (!_wasShown)
        {
            // Opened: as the game's window clears its search and category.
            _wasShown = true;
            SetSearch(ui!, "");
        }
        // As the game's window's update: a crafting station's window closes away from the station.
        if (Filter(ui!) is { } stationFilter && !stationFilter.CanRemainOpen())
        {
            NewCraftingUI.Close(quiet: false, returnToInventory: true);
            return;
        }
        if (Height != -Top - TabsHeight)
            Height = -Top - TabsHeight;
        Rectangle area = HudPoints.Units(this);
        bool overWindow = area.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;
        if (overWindow)
            Main.LocalPlayer.mouseInterface = true;

        UpdateContents(ui!);
        SetHovered(ui!, null);
        ClearMissing(ui!);
        Array.Clear(_available);
        for (int i = 0; i < Main.numAvailableRecipes; i++)
        {
            _available[Main.availableRecipe[i]] = true;
            _availableIndex[Main.availableRecipe[i]] = i;
        }
        PresentTabs(ui!);
        PresentHeader(ui!);
        object? hovered = PresentGrid(ui!);
        if (PlayerInput.UsingGamepad && hovered != null)
            SetSelected(ui!, EntryIndex(hovered));
        PresentCraft(ui!, hovered);
        PresentSearchTyping(ui!);
        // The crafting toggles (icon buttons, placed beside the coins by FreshUI), as the game's window draws them.
        CraftingUI.DrawGridToggle(Main.spriteBatch, 42, 285, 20030);
        if (!Main.InGuideCraftMenu)
            CraftingUI.DrawCraftFromNearbyChestsToggle(Main.spriteBatch, 82, 285, 20031);
    }

    private void PresentTabs(NewCraftingUI ui)
    {
        EntryFilterer<Item, IItemEntryFilter> filterer = Filterer(ui);
        List<IItemEntryFilter> filters = filterer.AvailableFilters;
        if (_tabCount != filters.Count)
        {
            foreach (FreshPanelButton old in _tabs)
                Children.Remove(old.Visual);
            _tabs.Clear();
            for (int i = 0; i < filters.Count; i++)
            {
                int index = i;
                IItemEntryFilter filter = filters[i];
                var tab = new FreshPanelButton();
                tab.Visual.WidthUnits = tab.Visual.HeightUnits = DimensionUnitType.Absolute;
                tab.Visual.Width = tab.Visual.Height = TabSize;
                tab.Text = "";
                if (filter.GetImage() is UIImageFramed image)
                {
                    tab.SetIcon(((ReLogic.Content.Asset<Texture2D>)ImageTexture.GetValue(image)!).Value, (Rectangle)ImageFrame.GetValue(image)!);
                    tab.CentreIcon();
                }
                tab.Click += (_, _) => ToggleFilter(index);
                FreshTooltip.Attach(tab.Visual, () => Language.GetTextValue(filter.GetDisplayNameKey()));
                _tabs.Add(tab);
                Children.Add(tab.Visual);
            }
            _tabCount = filters.Count;
            float left = MathF.Round((Width_ - (filters.Count * (TabSize + TabGap) - TabGap)) / 2);
            for (int i = 0; i < _tabs.Count; i++)
                _tabs[i].Visual.X = left + i * (TabSize + TabGap);
        }
        for (int i = 0; i < _tabs.Count; i++)
        {
            FreshPanelButton tab = _tabs[i];
            tab.IsChosen = filterer.IsFilterActive(i);
            if (HudPoints.Place(tab.Visual, 20052 + i, out bool focused))
                ToggleFilter(i);
            tab.IsLit = focused;
            if (focused)
                Main.instance.MouseTextNoOverride(Language.GetTextValue(filters[i].GetDisplayNameKey()), 0, 0);
        }
    }

    // As the game's tabs: one category at a time, clicking the open one closes it.
    private void ToggleFilter(int index)
    {
        if (Instance() is not { } ui)
            return;
        EntryFilterer<Item, IItemEntryFilter> filterer = Filterer(ui);
        if (!filterer.ActiveFilters.Contains(filterer.AvailableFilters[index]))
            filterer.ActiveFilters.Clear();
        filterer.ToggleFilter(index);
        ResetRecipes(ui);
    }

    private void PresentHeader(NewCraftingUI ui)
    {
        string title = Filter(ui)?.GetWindowDescription() ?? Lang.inter[25].Value;
        if (_title.Text != title)
            _title.Text = title;
        float width = Width_ - 3 * Pad - MathF.Ceiling(_title.GetAbsoluteWidth());
        if (_search.Visual.Width != width)
            _search.Visual.Width = width;
        bool caret = _writing && Main.instance.textBlinkerState == 1;
        string text = _searchText.Length > 0 || _writing ? _searchText + (caret ? "|" : "") : Language.GetTextValue("UI.PlayerNameSlot");
        if (_search.Text != text)
            _search.Text = text;
        Color textColor = _searchText.Length > 0 || _writing ? Color.White : FreshStyle.MutedText;
        if (_search.TextColor != textColor)
            _search.TextColor = textColor;
        _search.IsChosen = _writing;
        if (HudPoints.Place(_search.Visual, 20050, out bool focused, ref _searchPointX))
            ToggleSearch();
        _search.IsLit = focused;
        // Right-click clears the search and starts typing, as the game's.
        if (HudPoints.Hovered(_search.Visual) && Main.mouseRight && Main.mouseRightRelease)
        {
            Main.mouseRightRelease = false;
            SetSearch(ui, "");
            if (!_writing)
                ToggleSearch();
        }
        // A click elsewhere ends the typing.
        else if (_writing && (Main.mouseLeft && Main.mouseLeftRelease || Main.mouseRight && Main.mouseRightRelease) && !HudPoints.Hovered(_search.Visual))
        {
            StopSearch();
        }
    }

    private void ToggleSearch()
    {
        if (_writing)
        {
            StopSearch();
            return;
        }
        _writing = true;
        Main.clrInput();
        SoundEngine.PlaySound(12);
    }

    private void StopSearch()
    {
        if (!_writing)
            return;
        _writing = false;
        Main.clrInput();
        PlayerInput.WritingText = false;
        Main.instance.HandleIME();
    }

    // The game's typing, as its search box's: hotkeys off, IME, Enter or Esc to end.
    private void PresentSearchTyping(NewCraftingUI ui)
    {
        if (!_writing)
            return;
        PlayerInput.WritingText = true;
        Main.CurrentInputTextTakerOverride = SearchBar(ui);
        Main.instance.HandleIME();
        Main.instance.SetIMEPanelAnchor(new Vector2(HudPoints.Units(_search.Visual).Left, HudPoints.Units(_search.Visual).Bottom + 32), 0f);
        if (++Main.instance.textBlinkerCount >= 20)
        {
            Main.instance.textBlinkerState = (Main.instance.textBlinkerState + 1) % 2;
            Main.instance.textBlinkerCount = 0;
        }
        string text = Main.GetInputText(_searchText);
        if (text.Length > 40)
            text = text[..40];
        if (Main.inputTextEnter)
            StopSearch();
        else if (Main.inputTextEscape)
        {
            Main.inputTextEscape = false;
            StopSearch();
        }
        SetSearch(ui, text);
    }

    private void SetSearch(NewCraftingUI ui, string text)
    {
        if (text == _searchText)
            return;
        _searchText = text;
        // As the game's window's search changing.
        Filterer(ui).SetSearchFilter(text);
        ResetRecipes(ui);
    }

    // The recipes in the grid, the one under the game's cursor handled as the game's; returns it.
    private object? PresentGrid(NewCraftingUI ui)
    {
        IList entries = FilteredRecipes(ui);
        bool changed = entries.Count != _shownCount;
        for (int i = 0; !changed && i < entries.Count; i++)
            changed = !ReferenceEquals(entries[i], _shownEntries[i]);
        if (changed)
        {
            if (_shownEntries.Length < entries.Count)
                Array.Resize(ref _shownEntries, Math.Max(entries.Count, _shownEntries.Length * 2));
            for (int i = 0; i < entries.Count; i++)
                _shownEntries[i] = entries[i];
            _shownCount = entries.Count;
            while (_gridSlots.Count < _shownCount)
            {
                FreshItemSlot slot = Slot(GridScale);
                _gridSlots.Add(slot);
                _grid.InnerPanel.Children.Add(slot.Visual);
            }
            for (int i = 0; i < _gridSlots.Count; i++)
            {
                bool visible = i < _shownCount;
                if (_gridSlots[i].Visual.Visible != visible)
                    _gridSlots[i].Visual.Visible = visible;
            }
        }
        float gridTop = Main.InGuideCraftMenu ? 82 : 52;
        if (_separator.Y != gridTop - 4)
        {
            _separator.Y = gridTop - 4;
            _grid.Visual.Y = gridTop;
            _grid.Visual.Height = -gridTop - Pad;
        }
        Rectangle view = HudPoints.Units(_grid.Visual);
        int? selected = Selected(ui);
        bool gridFocused = PlayerInput.UsingGamepad && UILinkPointNavigator.CurrentPoint is >= 20100 and < 21000;
        object? hovered = null;
        for (int i = 0; i < _shownCount; i++)
        {
            object entry = _shownEntries[i]!;
            int recipe = EntryIndex(entry);
            FreshItemSlot slot = _gridSlots[i];
            Rectangle area = HudPoints.Units(slot.Visual);
            int point = 20100 + i;
            if (point < 21000)
            {
                UILinkPointNavigator.SetPosition(point, area.Center.ToVector2());
                // The gamepad's slot in view.
                if (gridFocused && UILinkPointNavigator.CurrentPoint == point && !view.Contains(area))
                    ScrollTo(area, view);
            }
            bool over = view.Contains(Main.mouseX, Main.mouseY) && area.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface;
            if (over)
            {
                hovered = entry;
                SetHovered(ui, entry);
                Main.LocalPlayer.mouseInterface = true;
                Main.inventoryScale = GridScale;
                HandleCraftSlot(ui, entry, 41);
            }
            // The chosen recipe's slot is gold (the gamepad's: its own, by the outline).
            if (gridFocused ? over : recipe == selected)
                ItemSlot.DrawGoldBGForCraftingMaterial = true;
            slot.SetItem(Main.recipe[recipe].createItem, 22, 0, GridScale, _available[recipe] ? null : Disabled);
            slot.IsGamepadFocused = gridFocused && UILinkPointNavigator.CurrentPoint == point;
        }
        return hovered;
    }

    private void ScrollTo(Rectangle area, Rectangle view)
    {
        double by = area.Top < view.Top ? (area.Top - view.Top) / 2.0 : (area.Bottom - view.Bottom) / 2.0;
        _grid.VerticalScrollBarValue = Math.Clamp(_grid.VerticalScrollBarValue + by, 0, _grid.VerticalScrollBarMaximum);
    }

    // The chosen (or hovered) recipe and its ingredients; with the Guide, the item to look up.
    private void PresentCraft(NewCraftingUI ui, object? hovered)
    {
        object? entry = hovered ?? SelectedEntry(ui);
        bool guide = Main.InGuideCraftMenu;
        if (_guide.Visual.Visible != guide)
        {
            _guide.Visual.Visible = guide;
            _guideText.Visible = guide;
        }
        if (guide)
        {
            float guideTop = CraftTop + _craft.Size + 14;
            if (_guide.Visual.Y != guideTop)
            {
                _guide.Visual.Y = guideTop;
                _guideText.Y = guideTop + MathF.Round((_guide.Size - 9) / 2);
            }
            if (HudPoints.Hovered(_guide.Visual))
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.inventoryScale = CraftScale;
                ItemSlot.Handle(ref Main.guideItem, 7);
            }
            HudPoints.Place(_guide.Visual, 20020, out _);
            _guideItem[0] = Main.guideItem;
            _guide.SetItem(Main.guideItem, 7, 0, CraftScale);
            string text = Main.guideItem.IsAir ? Lang.inter[24].Value : Lang.inter[21].Value + " " + Main.guideItem.Name;
            if (_guideText.Text != text)
                _guideText.Text = text;
        }
        bool shown = entry != null;
        if (_craft.Visual.Visible != shown)
        {
            _craft.Visual.Visible = shown;
            _arrow.Visible = shown;
        }
        int count = 0;
        if (entry != null)
        {
            int index = EntryIndex(entry);
            Recipe recipe = Main.recipe[index];
            if (HudPoints.Hovered(_craft.Visual))
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.inventoryScale = CraftScale;
                HandleCraftSlot(ui, entry, 42);
            }
            HudPoints.Place(_craft.Visual, 20000, out _);
            _craft.SetItem(recipe.createItem, 22, 0, CraftScale, _available[index] ? null : Disabled);
            while (count < Recipe.maxRequirements && !recipe.requiredItem[count].IsAir)
                count++;
            // Up to eleven ingredients at full size; more get smaller to fit.
            float fit = Math.Min(11f / Math.Max(count, 1), 1f);
            float scale = MaterialScale * fit, size = FreshItemSlot.SizeAt(scale), left = _arrow.X + _arrow.Width + 3;
            for (int j = 0; j < count; j++)
            {
                Item item = recipe.requiredItem[j];
                int owned = Recipe.GetAvailableItemCount(recipe.requiredItemQuickLookup[j]);
                bool enough = guide || owned >= item.stack;
                FreshItemSlot slot = _materials[j];
                float x = left + j * MathF.Round(17 * fit);
                if (slot.Visual.X != x || slot.Visual.Y != CraftTop)
                {
                    slot.Visual.X = x;
                    slot.Visual.Y = CraftTop;
                }
                if (!slot.Visual.Visible)
                    slot.Visual.Visible = true;
                UILinkPointNavigator.Shortcuts.NewCraftingUI_MaterialIndex = j;
                if (HudPoints.Hovered(slot.Visual))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    ItemSlot.HoverOverrideClick(item, 43);
                    CraftingUI.SetRecipeMaterialDisplayName(recipe, item);
                }
                HudPoints.Place(slot.Visual, 20001 + j, out _);
                slot.SetItem(item, 22, 0, scale, enough ? null : Disabled);
                TextRuntime ownedText = _owned[j];
                if (ownedText.Visible == guide)
                    ownedText.Visible = !guide;
                if (!guide)
                {
                    int shownOwned = Math.Min(owned, 1000);
                    if (_ownedShown[j] != shownOwned)
                    {
                        _ownedShown[j] = shownOwned;
                        ownedText.Text = owned > 999 ? "999+" : owned.ToString();
                    }
                    Color color = enough ? Enough : NotEnough;
                    if (ownedText.Color != color)
                        ownedText.Color = color;
                    float ownedX = x + 1, ownedY = CraftTop + size;
                    if (ownedText.X != ownedX || ownedText.Y != ownedY)
                    {
                        ownedText.X = ownedX;
                        ownedText.Y = ownedY;
                    }
                }
            }
            string requirements = guide ? Main.GetRecipeRequirementsText(recipe, explicitNone: false) : "";
            if (_requirements.Text != requirements)
                _requirements.Text = requirements;
            if (_requirements.Y != CraftTop + _craft.Size + 2)
                _requirements.Y = CraftTop + _craft.Size + 2;
        }
        else if (_requirements.Text != "")
        {
            _requirements.Text = "";
        }
        for (int j = count; j < _materials.Length; j++)
        {
            if (_materials[j].Visual.Visible)
            {
                _materials[j].Visual.Visible = false;
                _owned[j].Visible = false;
            }
        }
    }

    // The game's window's private state and methods.
    private static readonly Type EntryType = typeof(NewCraftingUI).GetNestedType("RecipeEntry", BindingFlags.NonPublic)!;
    private static readonly Func<NewCraftingUI?> Instance = Static<NewCraftingUI?>("_instance");
    private static readonly Func<NewCraftingUI, NewCraftingUI.RecipeFilter?> Filter = Field<NewCraftingUI.RecipeFilter?>("_filter");
    private static readonly Func<NewCraftingUI, EntryFilterer<Item, IItemEntryFilter>> Filterer = Field<EntryFilterer<Item, IItemEntryFilter>>("_filterer");
    private static readonly Func<NewCraftingUI, int?> Selected = Field<int?>("_selectedRecipeIndex");
    private static readonly Func<NewCraftingUI, IList> FilteredRecipes = Field<IList>("_filteredRecipes");
    private static readonly Func<NewCraftingUI, UIWrappedSearchBar> SearchBar = Field<UIWrappedSearchBar>("_searchBar");
    private static readonly Func<NewCraftingUI, object?> SelectedEntry = Call<Func<NewCraftingUI, object?>>("get_SelectedEntry");
    private static readonly Action<NewCraftingUI> UpdateContents = Call<Action<NewCraftingUI>>("UpdateContents");
    private static readonly Action<NewCraftingUI> ResetRecipes = Call<Action<NewCraftingUI>>("ResetRecipes");
    private static readonly Action<NewCraftingUI, object, int> HandleCraftSlot = Call<Action<NewCraftingUI, object, int>>("HandleCraftSlot");
    private static readonly Action<NewCraftingUI, object?> SetHovered = Setter<object?>("_hoveredEntry");
    private static readonly Action<NewCraftingUI, int?> SetSelected = Setter<int?>("_selectedRecipeIndex");
    private static readonly Action<NewCraftingUI, string?> SetMissing = Setter<string?>("_missingRequirementsTooltipText");
    private static readonly Func<object, int> EntryIndex = EntryField("index");
    private static readonly FieldInfo ImageTexture = typeof(UIImageFramed).GetField("_texture", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ImageFrame = typeof(UIImageFramed).GetField("_frame", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static void ClearMissing(NewCraftingUI ui) => SetMissing(ui, null);

    private static FieldInfo PrivateField(string name) =>
        typeof(NewCraftingUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;

    private static Func<T> Static<T>(string name) =>
        Expression.Lambda<Func<T>>(Expression.Convert(Expression.Field(null, PrivateField(name)), typeof(T))).Compile();

    private static Func<NewCraftingUI, T> Field<T>(string name)
    {
        ParameterExpression ui = Expression.Parameter(typeof(NewCraftingUI));
        return Expression.Lambda<Func<NewCraftingUI, T>>(Expression.Convert(Expression.Field(ui, PrivateField(name)), typeof(T)), ui).Compile();
    }

    private static Action<NewCraftingUI, T> Setter<T>(string name)
    {
        FieldInfo field = PrivateField(name);
        ParameterExpression ui = Expression.Parameter(typeof(NewCraftingUI)), value = Expression.Parameter(typeof(T));
        return Expression.Lambda<Action<NewCraftingUI, T>>(Expression.Assign(Expression.Field(ui, field), Expression.Convert(value, field.FieldType)), ui, value).Compile();
    }

    // A private method as a delegate, its RecipeEntry parameters and result as object.
    private static TDelegate Call<TDelegate>(string name) where TDelegate : Delegate
    {
        MethodInfo method = typeof(NewCraftingUI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
        ParameterExpression ui = Expression.Parameter(typeof(NewCraftingUI));
        ParameterInfo[] parameters = method.GetParameters();
        ParameterExpression[] arguments = parameters.Select(p => Expression.Parameter(p.ParameterType == EntryType ? typeof(object) : p.ParameterType)).ToArray();
        Expression call = Expression.Call(ui, method, arguments.Select((a, i) => (Expression)Expression.Convert(a, parameters[i].ParameterType)));
        if (method.ReturnType != typeof(void))
            call = Expression.Convert(call, typeof(object));
        return Expression.Lambda<TDelegate>(call, new[] { ui }.Concat(arguments)).Compile();
    }

    private static Func<object, int> EntryField(string name)
    {
        ParameterExpression entry = Expression.Parameter(typeof(object));
        FieldInfo field = EntryType.GetField(name, BindingFlags.Public | BindingFlags.Instance)!;
        return Expression.Lambda<Func<object, int>>(Expression.Field(Expression.Convert(entry, EntryType), field), entry).Compile();
    }
}
