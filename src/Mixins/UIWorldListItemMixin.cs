using Aether.Modding;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;

namespace FreshUI.Mixins;

/// <summary>The world list's item buttons laid out at the size they're drawn at (<see cref="PixelImages.Respace" />).</summary>
[Mixin(typeof(UIWorldListItem))]
public static class UIWorldListItemMixin
{
    [Inject(".ctor(Terraria.IO.WorldFileData,System.Int32,System.Boolean,System.Boolean,System.Boolean)", At.RETURN)]
    private static void Respace(UIWorldListItem self, WorldFileData data, int orderInList, bool canBePlayed, bool hasBeenPlayedByActivePlayer, bool isNewlyGenerated, CallbackInfo callback) =>
        PixelImages.Respace(self);
}
