using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The crafting lists (Aether's <see cref="UIRecipeList"/>: the available recipes in a column that scrolls
/// to the focused one, smaller the further a recipe is from it, and the focused one's ingredients beside it;
/// and <see cref="UIRecipeGrid"/>: every available recipe in rows, with up and down arrows), each recipe and
/// ingredient a <c>FreshItemSlot</c> where the game placed it, hovered and clicked as the game's (through
/// <c>Main.HoverOverCraftingItemButton</c>), with the gamepad's crafting points as the game's.
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> RecipeListAsked = GumRoutes.GameScreen<UIRecipeList>();
    private static readonly Func<bool> RecipeGridAsked = GumRoutes.GameScreen<UIRecipeGrid>();
    private static readonly FieldInfo ListAdjY = typeof(UIRecipeList).GetField("_adjY", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ListMiddleY = typeof(UIRecipeList).GetField("_middleY", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly FieldInfo ListTipColor = typeof(UIRecipeList).GetField("_tipColor", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo AdjustRecipeOffsets = typeof(CraftingUI).GetMethod("AdjustRecipeOffsets", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private const int GridStep = 42, GridLeft = 310, GridTop = 340;

    // Each icon's one-item array, also its slot's owner; this frame's are the first _craftUsed.
    private readonly List<Item[]> _craftItems = [];
    private int _craftUsed;
    private TextRuntime? _filterTitle;

    private void PresentCrafting()
    {
        _craftUsed = 0;
        bool titled = false;
        if (IsReady && RecipeListAsked())
            titled = PresentRecipeList();
        if (IsReady && RecipeGridAsked())
            PresentRecipeGrid();
        _craftTitled = titled;
    }

    // Whether the crafting list's title shows this frame (the banners' list sets its own after).
    private bool _craftTitled;

    private void CraftIcon(Item item, Vector2 position, float scale)
    {
        if (_craftUsed == _craftItems.Count)
            _craftItems.Add(new Item[1]);
        Item[] items = _craftItems[_craftUsed++];
        items[0] = item;
        Main.inventoryScale = scale;
        Present(items, items, 22, 0, position, interactive: false);
    }

    private static bool IconHovered(Vector2 position, float scale) =>
        Main.mouseX >= position.X && Main.mouseX <= position.X + TextureAssets.InventoryBack.Width() * scale &&
        Main.mouseY >= position.Y && Main.mouseY <= position.Y + TextureAssets.InventoryBack.Height() * scale &&
        !PlayerInput.IgnoreMouseInterface;

    // As UIRecipeList.DrawIcons; true: the recipe filter's title shows.
    private bool PresentRecipeList()
    {
        int adjY = (int)ListAdjY.GetValue(UIRecipeList.Instance)!, middleY = (int)ListMiddleY.GetValue(UIRecipeList.Instance)!;
        float[] recipeY = CraftingUI.availableRecipeY;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        // A recipe filter's window names it (the plain list's "Crafting" isn't shown: the toggles say it).
        bool titled = Main.numAvailableRecipes > 0 && CraftingUI.RecipeFilterHack != null;
        if (titled)
            PresentFilterTitle(CraftingUI.RecipeFilterHack!.GetWindowDescription(), new Vector2(76, 414 + adjY), (Color)ListTipColor.GetValue(UIRecipeList.Instance)!);
        AdjustRecipeOffsets.Invoke(Main.craftingUI, null);
        for (int i = 0; i < Recipe.maxRecipes; i++)
        {
            if (i >= Main.numAvailableRecipes || Math.Abs(recipeY[i]) > middleY)
                continue;
            // Recipes shrink away from the focused one (all small while fast-scrolling).
            float scale = Main.PipsFastScroll ? 0.75f : Math.Max(0.75f, 100f / (Math.Abs(recipeY[i]) + 100f));
            var position = new Vector2((int)(46f - 26f * scale), (int)(410f + recipeY[i] * scale - 30f * scale + adjY));
            if (!Main.LocalPlayer.creativeInterface && IconHovered(position, scale))
                Main.HoverOverCraftingItemButton(i);
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = i == Main.focusRecipe ? 0 : -1;
            // The focused recipe's slot is gold (the slot takes the flag back).
            if (i == Main.focusRecipe && PlayerInput.SettingsForUI.HighlightThingsForMouse)
                ItemSlot.DrawGoldBGForCraftingMaterial = true;
            CraftIcon(Main.recipe[Main.availableRecipe[i]].createItem, position, scale);
        }
        if (Main.numAvailableRecipes <= 0)
            return titled;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        Recipe recipe = Main.recipe[Main.availableRecipe[Main.focusRecipe]];
        for (int j = 0; j < Recipe.maxRequirements; j++)
        {
            Item ingredient = recipe.requiredItem[j];
            if (ingredient.type == 0)
            {
                UILinkPointNavigator.Shortcuts.CRAFT_CurrentIngredientsCount = j + 1;
                break;
            }
            // The ingredients go while the list scrolls away from the focused recipe.
            if (Main.inventoryBack.A + 50 - Math.Abs(recipeY[Main.focusRecipe]) * 2f == 0f)
                break;
            var position = new Vector2(80 + j * 40, 380 + adjY);
            if (IconHovered(position, 0.6f))
            {
                Main.craftingHide = true;
                Main.LocalPlayer.mouseInterface = true;
                ItemSlot.HoverOverrideClick(ingredient, 22);
                CraftingUI.SetRecipeMaterialDisplayName(recipe, ingredient);
            }
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = 1 + j;
            CraftIcon(ingredient, position, 0.6f);
        }
        Main.inventoryScale = 0.6f;
        return titled;
    }

    // As UIRecipeGrid.DrawIcons: a click focuses a recipe in the list and closes the grid.
    private void PresentRecipeGrid()
    {
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        int perRow = (Main.screenWidth - GridLeft - 280) / GridStep;
        int rows = (Main.screenHeight - GridTop - 20) / GridStep;
        UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow = perRow;
        UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn = rows;
        int count = Main.numAvailableRecipes;
        if (Main.recStart > count - perRow * rows)
            Main.recStart = Math.Max(0, count - perRow * rows);
        int arrowX = GridLeft - 20, arrowY = GridTop + 2;
        if (Main.recStart > 0 && GridArrow(TextureAssets.CraftUpButton.Value, arrowX, arrowY))
            Main.recStart = Math.Max(0, Main.recStart - perRow);
        if (Main.recStart < count - perRow * rows && GridArrow(TextureAssets.CraftDownButton.Value, arrowX, arrowY + 20))
            Main.recStart = Math.Min(Main.recStart + perRow, count - perRow);
        int column = 0, row = 0;
        for (int i = Main.recStart; i < Recipe.maxRecipes && i < Main.numAvailableRecipes; i++)
        {
            var position = new Vector2(GridLeft + column * GridStep, GridTop + row * GridStep);
            Item result = Main.recipe[Main.availableRecipe[i]].createItem;
            if (IconHovered(position, 0.75f))
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeftRelease && Main.mouseLeft)
                {
                    Main.focusRecipe = i;
                    Main.PipsFastScroll = true;
                    Main.PipsUseGrid = false;
                    SoundEngine.PlaySound(12);
                    Main.mouseLeftRelease = false;
                    if (PlayerInput.UsingGamepadUI)
                    {
                        UILinkPointNavigator.ChangePage(9);
                        Main.LockCraftingForThisCraftClickDuration();
                    }
                }
                Main.craftingHide = true;
                Main.HoverItem = result.Clone();
                ItemSlot.MouseHover(22);
                Main.hoverItemName = result.Name + (result.stack > 1 ? " (" + result.stack + ")" : "");
            }
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = i - Main.recStart;
            CraftIcon(result, position, 0.75f);
            if (++column >= perRow)
            {
                column = 0;
                if (++row >= rows)
                    break;
            }
        }
        Main.inventoryScale = 0.75f;
    }

    // An arrow beside the grid, drawn over the slots; true: clicked.
    private static bool GridArrow(Microsoft.Xna.Framework.Graphics.Texture2D texture, int x, int y)
    {
        DrawOver(texture, new Vector2(x, y), new Color(200, 200, 200, 200));
        if (Main.mouseX < x || Main.mouseX > x + texture.Width || Main.mouseY < y || Main.mouseY > y + texture.Height || PlayerInput.IgnoreMouseInterface)
            return false;
        Main.LocalPlayer.mouseInterface = true;
        if (!Main.mouseLeftRelease || !Main.mouseLeft)
            return false;
        SoundEngine.PlaySound(12);
        Main.mouseLeftRelease = false;
        return true;
    }

    private void PresentFilterTitle(string text, Vector2 position, Color color)
    {
        if (_filterTitle == null)
        {
            _filterTitle = new TextRuntime
            {
                WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
                HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
                FontSize = 9.5f,
                HasEvents = false,
            };
            GumText.SetOutline(_filterTitle, 0.5f, Color.Black);
            Children.Add(_filterTitle);
        }
        SetText(_filterTitle, text);
        float x = MathF.Round(position.X / 2), y = MathF.Round(position.Y / 2);
        if (_filterTitle.X != x || _filterTitle.Y != y)
        {
            _filterTitle.X = x;
            _filterTitle.Y = y;
        }
        if (_filterTitle.Color != color)
            _filterTitle.Color = color;
    }
}
