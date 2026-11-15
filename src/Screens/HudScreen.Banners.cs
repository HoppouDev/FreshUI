using System.Linq.Expressions;
using System.Reflection;
using Aether.UI.Game;
using FreshUI.GumUi;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace FreshUI.Screens;

/// <summary>
/// The banners waiting to be claimed (Aether's <see cref="UIBannerList"/>, a column that scrolls to the focused
/// one as the crafting list does, and <see cref="UIBannerGrid"/>, all of them in rows with arrows), each a
/// <c>FreshItemSlot</c> with the claimable count, as the crafting lists' slots: a click focuses a banner, holding
/// on the focused one claims it, and in the grid a click focuses it in the list. The banners' state stays the
/// game's (<c>BannerClaimingUI</c>).
/// </summary>
public sealed partial class HudScreen
{
    private static readonly Func<bool> BannerListAsked = GumRoutes.GameScreen<UIBannerList>();
    private static readonly Func<bool> BannerGridAsked = GumRoutes.GameScreen<UIBannerGrid>();
    private static readonly Func<UIBannerList, BannerClaimingUI> ListBanners = Member<UIBannerList, BannerClaimingUI>(typeof(UIBannerList), "_banners");
    private static readonly Func<UIBannerList, int> BannerAdjY = Member<UIBannerList, int>(typeof(UIBannerList), "_adjY");
    private static readonly Func<UIBannerList, int> BannerMiddleY = Member<UIBannerList, int>(typeof(UIBannerList), "_middleY");
    private static readonly Func<UIBannerList, Color> BannerTipColor = Member<UIBannerList, Color>(typeof(UIBannerList), "_tipColor");
    private static readonly Func<UIBannerGrid, BannerClaimingUI> GridBanners = Member<UIBannerGrid, BannerClaimingUI>(typeof(UIBannerGrid), "_banners");
    private static readonly Func<BannerClaimingUI, int> ClaimableCount = BannerCall<Func<BannerClaimingUI, int>>("UpdateAndGetClaimableItemsCount");
    private static readonly Action<BannerClaimingUI, int> MoveBannerList = BannerCall<Action<BannerClaimingUI, int>>("DrawBannersList_Move");
    private static readonly BannerField<float> ElementOffset = new("_currentElementOffset");
    private static readonly BannerField<int> FocusedBanner = new("_focusedElementIndex");
    private static readonly BannerField<int> StackLimiter = new("_bannerStackRequestLimiter");
    private static readonly BannerField<int> GridStart = new("_gridStartingElementIndex");
    private static readonly BannerField<int> GridCandidate = new("_gridSelectionCandidateIndex");
    private static readonly Func<BannerClaimingUI, int, int> EntryType = EntryMember("Type");
    private static readonly Func<BannerClaimingUI, int, int> EntryStack = EntryMember("Stack");
    private static readonly Func<BannerClaimingUI, int, int> EntryBanner = EntryMember("Index");
    private static readonly Action<BannerClaimingUI, int> ClaimBanner = ClaimAction();

    private void PresentBanners()
    {
        bool titled = false;
        if (IsReady && BannerListAsked())
            titled = PresentBannerList(ListBanners(UIBannerList.Instance));
        if (IsReady && BannerGridAsked())
            PresentBannerGrid(GridBanners(UIBannerGrid.Instance));
        // (The crafting list's filter title is the same label; it's the banners' while they show.)
        if (titled)
            PresentFilterTitle(Language.GetTextValue("GameUI.BannersTitle"), new Vector2(76, 414 + BannerAdjY(UIBannerList.Instance)), BannerTipColor(UIBannerList.Instance));
        bool anyTitle = titled || _craftTitled;
        if (_filterTitle != null && _filterTitle.Visible != anyTitle)
            _filterTitle.Visible = anyTitle;
    }

    // A banner's slot: the banner item with the claimable count as its stack.
    private void BannerIcon(BannerClaimingUI ui, int i, Item item, Vector2 position, float scale)
    {
        int stack = item.stack;
        item.stack = EntryStack(ui, i);
        ItemSlot.DrawNewAndShinyHighlightOverride = !ItemSlot.DrawGoldBGForCraftingMaterial && BannerSystem.IsNewBanner(EntryBanner(ui, i));
        if (_craftUsed == _craftItems.Count)
            _craftItems.Add(new Item[1]);
        Item[] items = _craftItems[_craftUsed++];
        items[0] = item;
        Main.inventoryScale = scale;
        Present(items, items, 35, 0, position, interactive: false);
        ItemSlot.DrawNewAndShinyHighlightOverride = false;
        item.stack = stack;
    }

    // As UIBannerList.DrawIcons; true: the list shows (and its title).
    private bool PresentBannerList(BannerClaimingUI ui)
    {
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        int count = ClaimableCount(ui);
        BannerSystem.AnyNewClaimableBanners = false;
        if (count <= 0)
        {
            Main.TryChangePipsPage(Main.PipPage.Recipes);
            return false;
        }
        int adjY = BannerAdjY(UIBannerList.Instance), middleY = BannerMiddleY(UIBannerList.Instance);
        MoveBannerList(ui, count);
        if (Main.PipsFastScroll)
        {
            ElementOffset.Set(ui, FocusedBanner.Get(ui));
            Main.PipsFastScroll = false;
        }
        bool pressing = Main.mouseLeft || Main.mouseRight;
        if (!pressing)
            StackLimiter.Set(ui, 0);
        for (int i = 0; i < count; i++)
        {
            float offset = (i - ElementOffset.Get(ui)) * 65f;
            if (Math.Abs(offset) > middleY || !ContentSamples.ItemsByType.TryGetValue(EntryType(ui, i), out Item? item))
                continue;
            float scale = Math.Max(0.75f, 100f / (Math.Abs(offset) + 100f));
            var position = new Vector2((int)(46f - 26f * scale), (int)(410f + offset * scale - 30f * scale + adjY));
            if (!Main.LocalPlayer.creativeInterface && IconHovered(position, scale))
            {
                Main.craftingHide = true;
                Main.LocalPlayer.mouseInterface = true;
                BannerSystem.ClearNewBanner(EntryBanner(ui, i));
                if (pressing)
                {
                    // A click on another banner focuses it; holding on the focused one claims it.
                    if (FocusedBanner.Get(ui) != i)
                    {
                        if (Main.mouseLeft && Main.mouseLeftRelease || Main.mouseRight && Main.mouseRightRelease)
                        {
                            FocusedBanner.Set(ui, i);
                            StackLimiter.Set(ui, -1);
                        }
                    }
                    else if (offset == 0f)
                        ClaimBanner(ui, i);
                }
                ItemSlot.MouseHover(item, 35);
            }
            bool focused = i == FocusedBanner.Get(ui);
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = focused ? 0 : -1;
            if (focused && PlayerInput.SettingsForUI.HighlightThingsForMouse)
                ItemSlot.DrawGoldBGForCraftingMaterial = true;
            BannerIcon(ui, i, item, position, scale);
        }
        Main.inventoryScale = 0.6f;
        return true;
    }

    // As UIBannerGrid.DrawIcons: a click focuses that banner in the list and closes the grid.
    private void PresentBannerGrid(BannerClaimingUI ui)
    {
        int count = ClaimableCount(ui);
        BannerSystem.AnyNewClaimableBanners = false;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = -1;
        UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall = -1;
        int perRow = (Main.screenWidth - GridLeft - 280) / GridStep;
        int rows = (Main.screenHeight - GridTop - 20) / GridStep;
        UILinkPointNavigator.Shortcuts.CRAFT_IconsPerRow = perRow;
        UILinkPointNavigator.Shortcuts.CRAFT_IconsPerColumn = rows;
        if (GridStart.Get(ui) > count - perRow * rows)
            GridStart.Set(ui, Math.Max(0, count - perRow * rows));
        int arrowX = GridLeft - 20, arrowY = GridTop + 2;
        if (GridStart.Get(ui) > 0 && GridArrow(TextureAssets.CraftUpButton.Value, arrowX, arrowY))
            GridStart.Set(ui, Math.Max(0, GridStart.Get(ui) - perRow));
        if (GridStart.Get(ui) < count - perRow * rows && GridArrow(TextureAssets.CraftDownButton.Value, arrowX, arrowY + 20))
            GridStart.Set(ui, Math.Min(GridStart.Get(ui) + perRow, count - perRow));
        GridStart.Set(ui, Utils.Clamp(GridStart.Get(ui), 0, count - 1));
        GridCandidate.Set(ui, Utils.Clamp(GridCandidate.Get(ui), 0, count - 1));
        int column = 0, row = 0;
        for (int i = GridStart.Get(ui); i < count; i++)
        {
            if (!ContentSamples.ItemsByType.TryGetValue(EntryType(ui, i), out Item? item))
                continue;
            var position = new Vector2(GridLeft + column * GridStep, GridTop + row * GridStep);
            if (IconHovered(position, 0.75f))
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeftRelease && Main.mouseLeft)
                {
                    FocusedBanner.Set(ui, i);
                    Main.PipsFastScroll = true;
                    Main.PipsUseGrid = false;
                    SoundEngine.PlaySound(12);
                    Main.mouseLeftRelease = false;
                    if (PlayerInput.UsingGamepadUI)
                    {
                        UILinkPointNavigator.ChangePage(22);
                        Main.LockCraftingForThisCraftClickDuration();
                    }
                }
                Main.craftingHide = true;
                Main.HoverItem = item.Clone();
                Main.HoverItem.stack = EntryStack(ui, i);
                ItemSlot.MouseHover(35);
                BannerSystem.ClearNewBanner(EntryBanner(ui, i));
                Main.hoverItemName = item.Name + (item.stack > 1 ? " (" + item.stack + ")" : "");
            }
            UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig = GridCandidate.Get(ui) != i ? -1 : 0;
            BannerIcon(ui, i, item, position, 0.75f);
            if (++column >= perRow)
            {
                column = 0;
                if (++row >= rows)
                    break;
            }
        }
        Main.inventoryScale = 0.75f;
    }

    /// <summary>A field of the game's banner claiming UI (internal), read and written.</summary>
    private sealed class BannerField<T>(string name)
    {
        private readonly FieldInfo _field = typeof(BannerClaimingUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
        private Func<BannerClaimingUI, T>? _get;
        private Action<BannerClaimingUI, T>? _set;

        public T Get(BannerClaimingUI ui) => (_get ??= Member<BannerClaimingUI, T>(typeof(BannerClaimingUI), name))(ui);

        public void Set(BannerClaimingUI ui, T value)
        {
            if (_set == null)
            {
                ParameterExpression owner = Expression.Parameter(typeof(BannerClaimingUI)), v = Expression.Parameter(typeof(T));
                _set = Expression.Lambda<Action<BannerClaimingUI, T>>(Expression.Assign(Expression.Field(owner, _field), v), owner, v).Compile();
            }
            _set(ui, value);
        }
    }

    private static TDelegate BannerCall<TDelegate>(string name) where TDelegate : Delegate =>
        (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), typeof(BannerClaimingUI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!);

    private static Expression BannerEntry(ParameterExpression ui, ParameterExpression index) =>
        Expression.ArrayIndex(Expression.Field(ui, typeof(BannerClaimingUI).GetField("_squashedItemTypesToShow", BindingFlags.NonPublic | BindingFlags.Instance)!), index);

    // An entry's field by its index in the banners to show.
    private static Func<BannerClaimingUI, int, int> EntryMember(string name)
    {
        ParameterExpression ui = Expression.Parameter(typeof(BannerClaimingUI)), index = Expression.Parameter(typeof(int));
        return Expression.Lambda<Func<BannerClaimingUI, int, int>>(Expression.Field(BannerEntry(ui, index), name), ui, index).Compile();
    }

    private static Action<BannerClaimingUI, int> ClaimAction()
    {
        ParameterExpression ui = Expression.Parameter(typeof(BannerClaimingUI)), index = Expression.Parameter(typeof(int));
        MethodInfo claim = typeof(BannerClaimingUI).GetMethod("TryClaimingBanner", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return Expression.Lambda<Action<BannerClaimingUI, int>>(Expression.Call(ui, claim, BannerEntry(ui, index)), ui, index).Compile();
    }
}
