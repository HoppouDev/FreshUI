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
    // Textures FreshUI makes (from the game's art, or drawn), by a key of their own: cleared by a hot reload.
    private static readonly Dictionary<string, Texture2D> MadeTextures = [];

    /// <summary>
    /// A texture FreshUI makes, made once by <paramref name="make"/> and kept under <paramref name="key"/> until a hot
    /// reload (<see cref="ClearMade"/>), so an edit to how it's made shows without a restart.
    /// </summary>
    public static Texture2D Made(string key, Func<Texture2D> make)
    {
        if (!MadeTextures.TryGetValue(key, out Texture2D? texture))
            MadeTextures[key] = texture = make();
        return texture;
    }

    /// <summary>Forgets the textures FreshUI made (after a hot reload; the screens rebuilt then make them again).</summary>
    internal static void ClearMade() => MadeTextures.Clear();

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
    /// A PNG in FreshUI's resources (<c>textures/components/knob.png</c>), loaded once. FreshUI's own art is a texel a UI
    /// pixel, opaque or clear, so its straight alpha is as premultiplied.
    /// </summary>
    public static Texture2D Resource(string path)
    {
        string key = "fresh-ui:" + path;
        if (!Loaded.TryGetValue(key, out Texture2D? texture))
        {
            using Stream stream = Aether.Modding.ModResources.OpenRead(Startup.ModId, path);
            Loaded[key] = texture = Texture2D.FromStream(Main.instance.GraphicsDevice, stream);
        }
        return texture;
    }

    /// <summary>
    /// <paramref name="source"/> nine-sliced to <paramref name="width"/>×<paramref name="height"/> texels: the
    /// <paramref name="corner"/> texels at each edge kept, the middle stretched or shrunk (nearest texel), for art drawn
    /// at its texels' size.
    /// </summary>
    public static Texture2D Sliced(Texture2D source, int width, int height, int corner)
    {
        var texels = new Microsoft.Xna.Framework.Color[source.Width * source.Height];
        source.GetData(texels);
        var sliced = new Microsoft.Xna.Framework.Color[width * height];
        for (int y = 0; y < height; y++)
        {
            int from = Slice(y, height, source.Height, corner);
            for (int x = 0; x < width; x++)
                sliced[y * width + x] = texels[from * source.Width + Slice(x, width, source.Width, corner)];
        }
        var texture = new Texture2D(source.GraphicsDevice, width, height);
        texture.SetData(sliced);
        return texture;
    }

    // The source texel for texel at of size: corners as they are, the middle spread over the source's middle.
    private static int Slice(int at, int size, int sourceSize, int corner)
    {
        if (at < corner)
            return at;
        if (at >= size - corner)
            return sourceSize - (size - at);
        return corner + (at - corner) * (sourceSize - 2 * corner) / (size - 2 * corner);
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
