using Aether.Modding;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;

namespace FreshUI.Mixins;

/// <summary>The character list's item buttons laid out at the size they're drawn at (<see cref="PixelImages.Respace" />).</summary>
[Mixin(typeof(UICharacterListItem))]
public static class UICharacterListItemMixin
{
    [Inject(".ctor(Terraria.IO.PlayerFileData,System.Int32)", At.RETURN)]
    private static void Respace(UICharacterListItem self, PlayerFileData data, int orderInList, CallbackInfo callback) => PixelImages.Respace(self);
}
