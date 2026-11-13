using Aether.Modding;
using Aether.UI.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace FreshUI.Legacy.Mixins;

/// <summary>
/// The hotbar at the bottom centre of the screen: inventory-sized slots in fixed places (the selected
/// one doesn't grow, so nothing moves while scrolling), and the held item's name above them at half
/// size, shown when the held item changes and then faded out.
/// </summary>
[Mixin(typeof(UIHotbar))]
public class UIHotbarMixin
{
#pragma warning disable CS0649, CS8618
    [Shadow]
    private UIHotbarSlot[] _slots;

    [Shadow]
    private int _heldSlotX;
#pragma warning restore CS0649, CS8618

    private long _freshClock;

    private float _freshNameTicks;

    private int _freshNameSlot;

    private int _freshNameType;

    private int _freshNamePrefix;

    [Shadow]
    private string SelectedName(Item item) => throw new NotSupportedException();

    // After the legacy layout (which also animates Main.hotbarScale, now only the selected slot's
    // fade-in), before the hotbar recalculates.
    [Inject("UpdateState()", At.INVOKE, Target = "Terraria.UI.UIElement::Recalculate()")]
    private void LayOut(CallbackInfo callback)
    {
        // On the art's pixel grid (2 UI units a pixel), like the inventory's slots.
        float width = Hud.HotbarSlotSize;
        float height = width;
        float total = width * 10f + Hud.SlotGap * 9f;
        float x = MathF.Round((Main.screenWidth - total) / 4f) * 2f;
        Hud.Bottom = MathF.Floor((Main.screenHeight - Hud.BottomMargin - height) / 2f) * 2f + height;
        for (int i = 0; i < _slots.Length; i++)
        {
            UIHotbarSlot slot = _slots[i];
            slot.Left.Set(x + i * (width + Hud.SlotGap), 0f);
            slot.Top.Set(Hud.Bottom - height, 0f);
            slot.Width.Set(width, 0f);
            slot.Height.Set(height, 0f);
        }
        _heldSlotX = (int)(x + total + Hud.SlotGap);
    }

    [Inject("DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch)", At.HEAD, Cancellable = true)]
    private void DrawName(SpriteBatch spriteBatch, CallbackInfo callback)
    {
        callback.Cancel();
        Player player = Main.LocalPlayer;
        Item selected = player.inventory[player.selectedItem];
        // Whatever is held when the hotbar appears (entering a world, closing the inventory) is
        // taken as is: only a change while it's shown shows the name.
        bool appearing = _freshClock == 0;
        float ticks = Hud.TicksSince(ref _freshClock);
        if (appearing || ticks > Hud.HotbarGap)
        {
            _freshNameSlot = player.selectedItem;
            _freshNameType = selected.type;
            _freshNamePrefix = selected.prefix;
            _freshNameTicks = Hud.NameShown + Hud.NameFade;
        }
        else if (player.selectedItem != _freshNameSlot || selected.type != _freshNameType || selected.prefix != _freshNamePrefix)
        {
            _freshNameSlot = player.selectedItem;
            _freshNameType = selected.type;
            _freshNamePrefix = selected.prefix;
            _freshNameTicks = 0f;
        }
        else
        {
            _freshNameTicks += ticks;
        }
        float opacity = 1f - MathHelper.Clamp((_freshNameTicks - Hud.NameShown) / Hud.NameFade, 0f, 1f);
        if (opacity <= 0f)
            return;
        string text = !string.IsNullOrEmpty(selected.Name) ? SelectedName(selected) : Lang.inter[37].Value;
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        float half = font.MeasureString(text).X * Hud.NameScale / 2f;
        var position = new Vector2(MathF.Round(Main.screenWidth / 2f - half), Hud.Bottom - Hud.HotbarSlotSize - Hud.NameSpace - Hud.NameLift);
        spriteBatch.DrawString(font, text, position, Hud.NameColor(selected) * opacity, 0f, default, Hud.NameScale, SpriteEffects.None, 0f);
    }

    // The held item's own slot (when it isn't on the hotbar), after the slots at their size.
    [ModifyConstant("DrawChildren(Microsoft.Xna.Framework.Graphics.SpriteBatch)", "float:20")]
    private static float HeldSlotTop(float top) => Hud.Bottom - Hud.HotbarSlotSize;

    [ModifyConstant("DrawChildren(Microsoft.Xna.Framework.Graphics.SpriteBatch)", "float:1")]
    private static float HeldSlotScale(float scale) => Hud.HotbarScale;
}
