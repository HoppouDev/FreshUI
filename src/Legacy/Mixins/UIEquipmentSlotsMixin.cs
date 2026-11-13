using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Terraria;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The equipment page on the art's pixel grid: columns and rows 46 apart (vanilla's 47 and 56 × 0.85,
/// truncated, plus a gap before the accessories), so its 44-unit slots have one art pixel between them
/// all, at even UI units.
/// </summary>
[Mixin(typeof(UIEquipmentSlots))]
public class UIEquipmentSlotsMixin
{
#pragma warning disable CS0649
    [Shadow]
    private int _top;
#pragma warning restore CS0649

    private const float VanillaPitch = 56f * 0.85f;

    [Shadow]
    private void AddSlot(Func<Item[]> items, int slot, int context, Vector2 position, float scale) => throw new NotSupportedException();

    [ModifyConstant("ColumnX(System.Int32)", "int:47")]
    private static int ColumnPitch(int pitch) => (int)PixelSlots.Pitch;

    [Inject("ColumnX(System.Int32)", At.RETURN, Cancellable = true)]
    private static void EvenColumn(int column, CallbackInfoReturnable<int> callback) => callback.SetReturnValue(Even(callback.ReturnValue));

    // A row's top, vanilla's (row × 47.6, truncated, from top, plus a 4-unit gap before the
    // accessories) at 46 a row, with no extra gap: one art pixel between every slot.
    private int FreshRowY(int y, int top)
    {
        int row = (int)MathF.Round((y - top) / VanillaPitch);
        return Even(top + row * (int)PixelSlots.Pitch);
    }

    private static int Even(int value) => value - (value & 1);

    [Redirect("Build()", At.INVOKE, Target = "Aether.UI.Game.UIEquipmentSlots::AddSlot(System.Func`1<Terraria.Item[]>,System.Int32,System.Int32,Microsoft.Xna.Framework.Vector2,System.Single)")]
    private void PlaceSlot(UIEquipmentSlots page, Func<Item[]> items, int slot, int context, Vector2 position, float scale) =>
        AddSlot(items, slot, context, new Vector2(position.X, FreshRowY((int)position.Y, _top)), scale);

    // The accessories' visibility toggles, 2 above their rows and right of the page.
    [Redirect("Build()", At.INVOKE, Target = "Aether.UI.Game.UIEquipToggle::Place(Microsoft.Xna.Framework.Rectangle)")]
    private void PlaceToggle(UIEquipToggle toggle, Rectangle area)
    {
        area.Y = FreshRowY(area.Y + 2, _top) - 2;
        area.X = Even(area.X);
        toggle.Place(area);
    }

    [ModifyArg("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", Target = "Terraria.Main::DrawDefenseCounter(System.Int32,System.Int32)", Index = 1)]
    private int DefenseCounterY(int y) => FreshRowY(y, _top);
}
