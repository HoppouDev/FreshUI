using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace FreshUI.GumUi;

/// <summary>
/// The textures FreshUI's Gum components draw, by Terraria asset name (e.g. <c>Images/Inventory_Back</c>,
/// <c>Images/Item_22</c>): loaded from the game's own assets, as the game draws them (premultiplied),
/// once each. Call from the game thread.
/// </summary>
public static class GumTextures
{
    private static readonly Dictionary<string, Texture2D> Loaded = [];

    public static Texture2D Load(string name)
    {
        if (!Loaded.TryGetValue(name, out Texture2D? texture))
        {
            texture = Main.Assets.Request<Texture2D>(name, AssetRequestMode.ImmediateLoad).Value;
            Loaded[name] = texture;
        }
        return texture;
    }
}
