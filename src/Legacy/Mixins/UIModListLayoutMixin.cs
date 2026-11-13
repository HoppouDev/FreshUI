using Aether.Modding;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// Aether's Mods window laid out as FreshUI's settings window: no search icon before the search box
/// (clicking the box searches), and Open Mods Folder and Back at the bottom of the left panel, under the
/// mod count, rather than along the bottom of the window, so both panels reach down to where they were.
/// </summary>
[Mixin("Aether.UI.Menu.UIModList")]
public class UIModListLayoutMixin
{
#pragma warning disable CS0649, CS8618
    [Shadow]
    private UIList _list;

    [Shadow]
    private UIScrollbar _listScroll;

    [Shadow]
    private UIPanel _detailPanel;

    [Shadow]
    private UIWrappedSearchBar _search;

    [Shadow]
    private UIText _count;

    [Shadow]
    private UIElement _back;

    [Shadow]
    private UIElement _folder;
#pragma warning restore CS0649, CS8618

    [Inject("OnInitialize()", At.TAIL)]
    private void LayOut(CallbackInfo callback)
    {
        // As the settings window: the panels down to 44 units above the screen's bottom.
        const float bottomMargin = 44f;
        const float buttonHeight = 50f;
        const float gap = 10f;
        // The list's top (under the search box) and the mod count's line under it.
        const float listTop = 34f;
        const float countHeight = 24f;
        _search.HideSearchButton();
        // The window's gamepad links look for a "Search" point: the box is it now.
        _search.Children.First().SetSnapPoint("Search", 0);
        UIElement browser = _list.Parent;
        browser.Height.Set(-bottomMargin, 1f);
        _detailPanel.Height.Set(-bottomMargin, 1f);
        // Open Mods Folder above Back, filling the panel's width.
        UIElement[] buttons = [_folder, _back];
        for (int i = 0; i < buttons.Length; i++)
        {
            UIElement button = buttons[i];
            button.Remove();
            browser.Append(button);
            button.Width.Set(0f, 1f);
            button.HAlign = 0f;
            button.VAlign = 1f;
            button.Top.Set(-(buttons.Length - 1 - i) * (buttonHeight + gap), 0f);
            button.Height.Set(buttonHeight, 0f);
        }
        float stack = buttons.Length * (buttonHeight + gap);
        _count.Top.Set(-stack, 0f);
        float reserved = listTop + countHeight + stack;
        _list.Height.Set(-reserved, 1f);
        _listScroll.Height.Set(-reserved - 10f, 1f);
    }
}
