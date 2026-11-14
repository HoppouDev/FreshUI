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

    /// <summary>
    /// <paramref name="name"/> with its texel rows from <paramref name="row"/> to
    /// <paramref name="row"/> + <paramref name="count"/> repeated once, below themselves: art made taller
    /// by whole pixels, for a nine-slice to draw at that height without stretching its middle unevenly.
    /// </summary>
    public static Texture2D Taller(string name, int row, int count)
    {
        string key = $"{name}#{row}+{count}";
        if (Loaded.TryGetValue(key, out Texture2D? taller))
        {
            return taller;
        }
        Texture2D source = Load(name);
        int width = source.Width, height = source.Height;
        var texels = new Microsoft.Xna.Framework.Color[width * height];
        source.GetData(texels);
        var grown = new Microsoft.Xna.Framework.Color[width * (height + count)];
        Array.Copy(texels, 0, grown, 0, width * (row + count));
        Array.Copy(texels, width * row, grown, width * (row + count), width * (height - row));
        taller = new Texture2D(source.GraphicsDevice, width, height + count);
        taller.SetData(grown);
        Loaded[key] = taller;
        return taller;
    }
}
