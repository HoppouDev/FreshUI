using System.Text.Json;
using Aether.Modding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace FreshUI;

/// <summary>
/// Terraria's fonts with Andy Bold's glyphs drawn from a signed distance field (resources/fonts,
/// from tools/sdf_font.py) by FreshUI's SDF shader (Aether's <c>Texture.EffectEXT</c>), so text is
/// sharp at any size. Each font keeps its own spacing; characters the field lacks keep their images.
/// </summary>
public static class SdfText
{
    // Each font's Andy Bold, fitted to its glyphs: pixels per em, the ink's offset right of the pen
    // (font units) and the baseline below the line's top.
    private static readonly (Func<Asset<DynamicSpriteFont>> Font, float Size, float Left, float Baseline)[] Fonts =
    [
        (() => FontAssets.MouseText, 19f, 1f, 15.75f),
        (() => FontAssets.ItemStack, 16.75f, 1f, 13.62f),
        (() => FontAssets.DeathText, 49.5f, 2f, 39.88f),
        (() => FontAssets.CombatText[0], 21f, 1f, 16.75f),
        (() => FontAssets.CombatText[1], 25.75f, 1f, 20.88f)
    ];

    private static readonly HashSet<DynamicSpriteFont> Replaced = [];

    private static Texture2D? _atlas;

    private static Dictionary<char, (Rectangle Source, Vector2 Corner)>? _glyphs;

    private static float _em;

    private static bool _failed;

    /// <summary>Replaces the glyphs of every loaded font not replaced yet (fonts load late, and again with resource packs).</summary>
    public static void Update()
    {
        if (_failed || Main.instance?.GraphicsDevice == null)
            return;
        foreach (var (font, size, left, baseline) in Fonts)
        {
            Asset<DynamicSpriteFont>? asset = font();
            if (asset is not { IsLoaded: true } || asset.Value is not { } value || Replaced.Contains(value))
                continue;
            if (_atlas == null && !Load())
                return;
            Replace(value, size, left, baseline);
            Replaced.Add(value);
        }
    }

    private static bool Load()
    {
        try
        {
            GraphicsDevice device = Main.instance.GraphicsDevice;
            byte[] code;
            using (Stream stream = ModResources.OpenRead(Startup.ModId, "shaders/SdfText.fxb"))
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                code = memory.ToArray();
            }
            using (Stream stream = ModResources.OpenRead(Startup.ModId, "fonts/andy-sdf.png"))
                _atlas = Texture2D.FromStream(device, stream);
            _atlas.SamplerEXT = SamplerState.LinearClamp;
            _atlas.EffectEXT = new Effect(device, code);

            using Stream json = ModResources.OpenRead(Startup.ModId, "fonts/andy-sdf.json");
            using JsonDocument document = JsonDocument.Parse(json);
            _em = document.RootElement.GetProperty("em").GetSingle();
            _glyphs = [];
            foreach (JsonProperty glyph in document.RootElement.GetProperty("glyphs").EnumerateObject())
            {
                int[] g = glyph.Value.Deserialize<int[]>()!;
                _glyphs[(char)int.Parse(glyph.Name)] = (new Rectangle(g[0], g[1], g[2], g[3]), new Vector2(g[4], g[5]));
            }
            return true;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or ArgumentException)
        {
            _failed = true;
            Console.Error.WriteLine($"FreshUI could not load its SDF text: {exception.Message}");
            return false;
        }
    }

    private static void Replace(DynamicSpriteFont font, float size, float left, float baseline)
    {
        // Atlas pixels to font units.
        float scale = size / _em;
        foreach (var (character, (source, corner)) in _glyphs!)
        {
            if (!font.IsCharacterSupported(character))
                continue;
            // The font draws an image at its padding, after the character's left bearing.
            float bearing = font.GetCharacterMetrics(character).LeftPadding;
            var offset = new Vector2(left + corner.X * scale - bearing, baseline + corner.Y * scale);
            font.SetCharacterImage(character, _atlas!, source, offset, 1f / scale);
        }
    }
}
