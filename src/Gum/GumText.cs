using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace FreshUI.GumUi;

/// <summary>
/// Draws all Gum text in one font from a signed distance field atlas (baked by
/// tools/GenerateFonts.cs), scaled to each text's FontSize and shaded by GumText.fx through FNA's
/// <c>Texture.EffectEXT</c>, so text is sharp at any size.
/// <para>
/// Styles are procedural: a weight moves the drawn edge along the field (the shader's Edge), an
/// outline is a second edge further out, a gradient colours each line from the capitals' top to the
/// baseline, and italic shears each glyph's field about the baseline. Gum's drop shadow (HasDropshadow,
/// DropshadowColor, DropshadowOffsetX/Y, DropshadowBlur) draws from a copy of the glyphs below them
/// in the same texture, so a text and its shadow are one draw. The fill colour is Gum's Color.
/// </para>
/// <para>
/// Each style drawn is its own texture, since the shader's parameters belong to the texture it is
/// attached to; a style's texture is disposed once no text uses it.
/// </para>
/// </summary>
static partial class GumText
{
    // FreshUI's resources (tools/GenerateGumFont.cs bakes the font; shaders/GumText.fx is the shader).
    private const string FontPath = "gum/TextSdf.fnt";
    private const string AtlasPath = "gum/TextSdf_0.png";
    private const string BakePath = "gum/TextSdf.json";
    private const string EffectPath = "gum/GumText.fxb";

    // From the bake (TextSdf.json): the atlas's pixels per em, and pixels from a glyph's edge to the
    // end of its field (the field changes by 1 / (2 * Spread) per pixel).
    private static float AtlasFontSize = 64;
    private static float Spread = 16;

    /// <summary>The CSS-style weight the font is drawn at as designed (its OS/2 weight class).</summary>
    public static float DesignWeight { get; private set; } = 400;

    /// <summary>How far a line's baseline is below its top, in ems: text placed by its baseline sits at Y + FontSize * Baseline.</summary>
    public static float Baseline => _upright.Baseline / AtlasFontSize;

    /// <summary>The weight of text with Gum's IsBold and no weight of its own: 700, or 200 above a bold font's.</summary>
    public static float BoldWeight => Math.Min(1000, Math.Max(700, DesignWeight + 200));

    /// <summary>Horizontal shift per pixel above the baseline of text with Gum's IsItalic (about 11°).</summary>
    public const float ItalicSlant = 0.2f;

    // Ems each side of a stroke moves per 100 of weight away from the design weight: roughly how a
    // typeface family's stems thicken from one weight to the next.
    private const float StrokePer100 = 0.0055f;

    // Styles are rounded to these, bounding the textures an animated style creates.
    private const int WeightStep = 25;
    private const float WidthStep = 0.25f;

    private static GraphicsDevice _device = null!;
    private static Effect _effect = null!;
    private static Atlas _upright = null!;
    private static Atlas? _italic;
    private static readonly Dictionary<StyleKey, Variant> Variants = [];
    private static readonly ConditionalWeakTable<TextRuntime, Style> Styles = new();
    private static readonly ConditionalWeakTable<Text, StrongBox<StyleKey>> Assigned = new();
    private static readonly ConditionalWeakTable<TextRuntime, object> Watched = new();

    /// <summary>
    /// Loads the atlas and shader from FreshUI's resources and takes over Gum's font loading. Call
    /// after GumService.Initialize.
    /// </summary>
    public static void Initialize(GraphicsDevice device)
    {
        _device = device;
        using (JsonDocument bake = JsonDocument.Parse(ReadResource(BakePath)))
        {
            AtlasFontSize = bake.RootElement.GetProperty("size").GetSingle();
            Spread = bake.RootElement.GetProperty("spread").GetSingle();
            DesignWeight = bake.RootElement.GetProperty("weight").GetSingle();
        }
        _effect = new Effect(device, ReadResource(EffectPath));
        using (Stream png = Aether.Modding.ModResources.OpenRead(Startup.ModId, AtlasPath))
        {
            _upright = Atlas.Load(device, System.Text.Encoding.UTF8.GetString(ReadResource(FontPath)).ReplaceLineEndings("\n").Split('\n'), png);
        }

        // Gum's own loader looks up a baked font per Font/FontSize; every text uses the atlas instead.
        GraphicalUiElement.UpdateFontFromProperties = UpdateFont;
    }

    private static byte[] ReadResource(string path)
    {
        using Stream stream = Aether.Modding.ModResources.OpenRead(Startup.ModId, path);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>
    /// The text's CSS-style weight, 1 to 1000: its own, else <see cref="BoldWeight"/> with Gum's
    /// IsBold, else <see cref="DesignWeight"/>.
    /// </summary>
    public static float GetFontWeight(TextRuntime text) =>
        Styles.TryGetValue(text, out Style? style) && style.Weight is float weight ? weight
        : text.IsBold ? BoldWeight
        : DesignWeight;

    /// <summary>Sets the text's CSS-style weight, 1 to 1000 (<see cref="DesignWeight"/> draws the font as designed).</summary>
    public static void SetFontWeight(TextRuntime text, float weight)
    {
        Styles.GetOrCreateValue(text).Weight = Math.Clamp(weight, 1, 1000);
        text.UpdateToFontValues();
    }

    /// <summary>The text's outline width in UI pixels (its own, else Gum's OutlineThickness) and colour.</summary>
    public static (float Width, Color Color) GetOutline(TextRuntime text)
    {
        Style? style = Styles.TryGetValue(text, out Style? found) ? found : null;
        return (style?.OutlineWidth ?? text.OutlineThickness, style?.OutlineColor ?? Color.Black);
    }

    /// <summary>
    /// Outlines the text: <paramref name="width"/> UI pixels (0 for none) of <paramref name="color"/>
    /// around each glyph, up to a quarter of the font size.
    /// </summary>
    public static void SetOutline(TextRuntime text, float width, Color color)
    {
        Style style = Styles.GetOrCreateValue(text);
        style.OutlineWidth = Math.Max(0, width);
        style.OutlineColor = color;
        text.UpdateToFontValues();
    }

    /// <summary>The text's vertical gradient, from the capitals' top to the baseline, if it has one.</summary>
    public static (Color Top, Color Bottom)? GetGradient(TextRuntime text) =>
        Styles.TryGetValue(text, out Style? style) ? style.Gradient : null;

    /// <summary>
    /// Colours each line of the text from <paramref name="top"/> at the capitals' top to
    /// <paramref name="bottom"/> at the baseline (null for none), multiplied by the text's Color.
    /// </summary>
    public static void SetGradient(TextRuntime text, (Color Top, Color Bottom)? gradient)
    {
        Styles.GetOrCreateValue(text).Gradient = gradient;
        text.UpdateToFontValues();
    }

    private static void UpdateFont(IText text, GraphicalUiElement element)
    {
        var renderable = (Text)text;
        var runtime = element as TextRuntime;
        float fontSize = runtime is { FontSize: > 0 } ? runtime.FontSize : AtlasFontSize;
        // Gum's oversampling scale: draws a font rasterized larger at the text's size, leaving the
        // public FontScale to compose on top. Only oversampling sets it, and that is off.
        float scale = fontSize / AtlasFontSize;

        StyleKey key = runtime != null ? KeyOf(runtime) : new StyleKey(Round(DesignWeight, WeightStep), false, 0, 0, null, null, false, 0);
        renderable.BitmapFont = Use(renderable, key);
        SetOversampleCompensationScale(renderable, scale);

        if (runtime != null)
        {
            ScaleShadowOffset(runtime, renderable, scale);
            if (!Watched.TryGetValue(runtime, out _))
            {
                Watched.Add(runtime, new object());
                runtime.PropertyChanged += OnPropertyChanged;
            }
        }
    }

    // Gum draws a shadow DropshadowOffset times the text's whole scale away, ours included, so the
    // offset given to the renderable leaves ours out: offsets stay in pixels at the text's size.
    private static void ScaleShadowOffset(TextRuntime runtime, Text renderable, float scale)
    {
        renderable.DropshadowOffsetX = runtime.DropshadowOffsetX / scale;
        renderable.DropshadowOffsetY = runtime.DropshadowOffsetY / scale;
    }

    // TextRuntime's dropshadow setters hand the renderable the unscaled offset after the font update.
    private static void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is TextRuntime runtime && e.PropertyName?.Contains("Dropshadow") == true
            && runtime.RenderableComponent is Text renderable)
        {
            ScaleShadowOffset(runtime, renderable, runtime.FontSize > 0 ? runtime.FontSize / AtlasFontSize : 1);
        }
    }

    private static StyleKey KeyOf(TextRuntime runtime)
    {
        (float outlineWidth, Color outlineColor) = GetOutline(runtime);
        (Color Top, Color Bottom)? gradient = GetGradient(runtime);
        int outline = Round(outlineWidth, WidthStep);
        return new StyleKey(
            Round(GetFontWeight(runtime), WeightStep),
            runtime.IsItalic,
            outline,
            outline > 0 ? outlineColor.PackedValue : 0,
            gradient?.Top.PackedValue,
            gradient?.Bottom.PackedValue,
            runtime.HasDropshadow,
            runtime.HasDropshadow ? Round(runtime.DropshadowBlur, WidthStep) : 0);
    }

    private static int Round(float value, float step) => (int)MathF.Round(value / step);

    /// <summary>The style's font for <paramref name="text"/>, releasing the one it had.</summary>
    private static BitmapFont Use(Text text, StyleKey key)
    {
        if (Assigned.TryGetValue(text, out StrongBox<StyleKey>? current))
        {
            if (current.Value == key && Variants.TryGetValue(key, out Variant? same))
            {
                return same.Font;
            }
            Release(current.Value);
        }

        if (!Variants.TryGetValue(key, out Variant? variant))
        {
            variant = CreateVariant(key);
            Variants[key] = variant;
        }
        variant.Users++;
        Assigned.AddOrUpdate(text, new StrongBox<StyleKey>(key));
        return variant.Font;
    }

    private static void Release(StyleKey key)
    {
        if (Variants.TryGetValue(key, out Variant? variant) && --variant.Users <= 0)
        {
            Variants.Remove(key);
            variant.Texture.EffectEXT?.Dispose();
            variant.Texture.Dispose();
        }
    }

    private static Variant CreateVariant(StyleKey key)
    {
        Atlas atlas = key.Italic ? _italic ??= _upright.Slanted(ItalicSlant) : _upright;
        // How far each side of a stroke moves, in atlas pixels.
        float grow = (key.Weight * WeightStep - DesignWeight) / 100 * StrokePer100 * AtlasFontSize;

        // A shadow draws from a copy of the glyphs below them, so it shares their texture and draw.
        int height = key.Shadow ? atlas.Height * 2 : atlas.Height;
        var field = new byte[atlas.Width * height];
        atlas.Field.CopyTo(field, 0);
        if (key.Shadow)
        {
            atlas.Field.CopyTo(field, atlas.Field.Length);
        }

        var texture = new Texture2D(_device, atlas.Width, height, false, SurfaceFormat.Alpha8);
        texture.SetData(field);
        texture.SamplerEXT = SamplerState.LinearClamp;

        Effect effect = _effect.Clone();
        EffectParameterCollection parameters = effect.Parameters;
        parameters["Edge"].SetValue(0.5f - grow / (2 * Spread));
        parameters["FieldPerTexel"].SetValue(1 / (2 * Spread));
        parameters["TextureSize"].SetValue(new Vector2(atlas.Width, height));
        parameters["RowHeight"].SetValue((float)atlas.RowHeight);
        parameters["RowTop"].SetValue((float)atlas.RowTop);
        parameters["GradientStart"].SetValue((float)atlas.CapTop);
        parameters["GradientEnd"].SetValue((float)atlas.Baseline);
        parameters["GradientTop"].SetValue(Premultiplied(key.GradientTop ?? uint.MaxValue));
        parameters["GradientBottom"].SetValue(Premultiplied(key.GradientBottom ?? uint.MaxValue));
        SetPixelWidths(effect, key);
        parameters["OutlineColor"].SetValue(Premultiplied(key.OutlineColor));
        parameters["ShadowStart"].SetValue(key.Shadow ? atlas.Height : float.MaxValue);
        texture.EffectEXT = effect;

        // Bolder strokes take more room, so letters advance by what both sides grew.
        int advanceGrowth = (int)MathF.Round(2 * grow);
        var font = new BitmapFont([texture], atlas.ToFnt(advanceGrowth, height, yShift: 0));
        if (key.Shadow)
        {
            font.ShadowFont = new BitmapFont([texture], atlas.ToFnt(advanceGrowth, height, yShift: atlas.Height));
        }
        return new Variant(font, texture);
    }

    /// <summary>Converts every style's outline and blur widths (UI pixels) to screen pixels at the current <see cref="UiPixels.Scale"/>.</summary>
    public static void RefreshPixelScale()
    {
        foreach ((StyleKey key, Variant variant) in Variants)
        {
            SetPixelWidths(variant.Texture.EffectEXT, key);
        }
    }

    // The shader measures widths in screen pixels.
    private static void SetPixelWidths(Effect effect, StyleKey key)
    {
        effect.Parameters["OutlineWidth"].SetValue(key.Outline * WidthStep * UiPixels.Scale);
        effect.Parameters["ShadowBlur"].SetValue(key.ShadowBlur * WidthStep * UiPixels.Scale);
    }

    private static Vector4 Premultiplied(uint packed)
    {
        Vector4 color = new Color { PackedValue = packed }.ToVector4();
        return new Vector4(color.X * color.W, color.Y * color.W, color.Z * color.W, color.W);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_OversampleCompensationScale")]
    private static extern void SetOversampleCompensationScale(Text text, float value);

    private sealed class Style
    {
        public float? Weight;
        public float? OutlineWidth;
        public Color? OutlineColor;
        public (Color Top, Color Bottom)? Gradient;
    }

    /// <summary>What a style's texture is drawn with: weight and widths in steps, colours packed.</summary>
    private readonly record struct StyleKey(
        int Weight, bool Italic, int Outline, uint OutlineColor,
        uint? GradientTop, uint? GradientBottom, bool Shadow, int ShadowBlur);

    private sealed class Variant(BitmapFont font, Texture2D texture)
    {
        public BitmapFont Font { get; } = font;

        public Texture2D Texture { get; } = texture;

        public int Users;
    }

    private readonly record struct Glyph(int Id, int X, int Y, int Width, int Height, int XOffset, int YOffset, int XAdvance);

    /// <summary>
    /// A distance field, one byte per pixel, and the BMFont description of its glyphs. Glyphs sit in
    /// rows of <see cref="RowHeight"/> at their place in the line (a row's first pixel is line y
    /// <see cref="RowTop"/>), so the shader can tell a pixel's height in its line for gradients.
    /// </summary>
    private sealed partial class Atlas
    {
        private readonly string[] _lines;

        private Atlas(byte[] field, int width, int height, string[] lines, Dictionary<int, Glyph> glyphs,
            int baseline, int rowHeight, int rowTop)
        {
            Field = field;
            Width = width;
            Height = height;
            _lines = lines;
            Glyphs = glyphs;
            Baseline = baseline;
            RowHeight = rowHeight;
            RowTop = rowTop;
        }

        public byte[] Field { get; }

        public int Width { get; }

        public int Height { get; }

        public int Baseline { get; }

        public int RowHeight { get; }

        public int RowTop { get; }

        /// <summary>The line y of the top of the capitals' ink (H's, inside its field's spread).</summary>
        public int CapTop => Glyphs.TryGetValue('H', out Glyph h) ? h.YOffset + (int)Spread : 0;

        private Dictionary<int, Glyph> Glyphs { get; }

        public static Atlas Load(GraphicsDevice device, string[] lines, Stream pngStream)
        {
            var glyphs = new Dictionary<int, Glyph>();
            int baseline = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith("char "))
                {
                    Dictionary<string, int> values = Values(line);
                    var glyph = new Glyph(values["id"], values["x"], values["y"], values["width"], values["height"],
                        values["xoffset"], values["yoffset"], values["xadvance"]);
                    glyphs[glyph.Id] = glyph;
                }
                else if (line.StartsWith("common "))
                {
                    baseline = Values(line)["base"];
                }
            }

            using Texture2D png = Texture2D.FromStream(device, pngStream);
            var pixels = new Color[png.Width * png.Height];
            png.GetData(pixels);
            // The baked field is grayscale; any channel holds the distance.
            byte[] field = Array.ConvertAll(pixels, pixel => pixel.R);

            // KernSmith packs glyphs tightly; lay them out in line-aligned rows instead.
            var baked = new Atlas(field, png.Width, png.Height, lines, glyphs, baseline, 0, 0);
            return baked.Pack(glyphs.Values.Select(glyph => (glyph, baked.Copy(glyph))).ToList());
        }

        /// <summary>A copy with each glyph sheared about the baseline: a pixel h above it moves right by slant * h.</summary>
        public Atlas Slanted(float slant)
        {
            var sheared = new List<(Glyph Glyph, byte[] Pixels)>();
            foreach (Glyph glyph in Glyphs.Values)
            {
                float topShift = slant * (Baseline - glyph.YOffset);
                float bottomShift = slant * (Baseline - glyph.YOffset - glyph.Height);
                int left = (int)MathF.Floor(bottomShift);
                int width = glyph.Width + (int)MathF.Ceiling(topShift) - left;
                var pixels = new byte[width * glyph.Height];
                for (int y = 0; y < glyph.Height; y++)
                {
                    float shift = slant * (Baseline - (glyph.YOffset + y + 0.5f)) - left;
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = SampleRow(glyph, y, x - shift);
                    }
                }
                sheared.Add((glyph with { Width = width, XOffset = glyph.XOffset + left }, pixels));
            }
            return Pack(sheared);
        }

        private byte[] Copy(Glyph glyph)
        {
            var pixels = new byte[glyph.Width * glyph.Height];
            for (int row = 0; row < glyph.Height; row++)
            {
                Array.Copy(Field, (glyph.Y + row) * Width + glyph.X, pixels, row * glyph.Width, glyph.Width);
            }
            return pixels;
        }

        /// <summary>The field along one of the glyph's rows at a fractional column, outside beyond it.</summary>
        private byte SampleRow(Glyph glyph, int y, float x)
        {
            int x0 = (int)MathF.Floor(x);
            float t = x - x0;
            float a = Pixel(glyph, x0, y);
            float b = Pixel(glyph, x0 + 1, y);
            return (byte)MathF.Round(a + (b - a) * t);
        }

        private byte Pixel(Glyph glyph, int x, int y) =>
            x < 0 || x >= glyph.Width ? (byte)0 : Field[(glyph.Y + y) * Width + glyph.X + x];

        /// <summary>
        /// Lays the glyphs out in rows as tall as the tallest reach of any glyph in the line, each at
        /// its own place in the line, into an atlas as wide as this one.
        /// </summary>
        private Atlas Pack(List<(Glyph Glyph, byte[] Pixels)> glyphs)
        {
            int rowTop = glyphs.Min(g => g.Glyph.YOffset);
            int rowHeight = glyphs.Max(g => g.Glyph.YOffset + g.Glyph.Height) - rowTop;
            glyphs.Sort((a, b) => a.Glyph.Id.CompareTo(b.Glyph.Id));

            var placed = new Dictionary<int, Glyph>();
            int x = 0, row = 0;
            foreach ((Glyph glyph, _) in glyphs)
            {
                if (x + glyph.Width > Width)
                {
                    (x, row) = (0, row + 1);
                }
                placed[glyph.Id] = glyph with { X = x, Y = row * rowHeight + glyph.YOffset - rowTop };
                x += glyph.Width + 1;
            }

            int height = (row + 1) * rowHeight;
            var field = new byte[Width * height];
            foreach ((Glyph glyph, byte[] pixels) in glyphs)
            {
                Glyph at = placed[glyph.Id];
                for (int y = 0; y < glyph.Height; y++)
                {
                    Array.Copy(pixels, y * glyph.Width, field, (at.Y + y) * Width + at.X, glyph.Width);
                }
            }
            return new Atlas(field, Width, height, _lines, placed, Baseline, rowHeight, rowTop);
        }

        /// <summary>
        /// The BMFont text for this atlas in a texture <paramref name="textureHeight"/> tall, with every
        /// advance changed by <paramref name="advanceGrowth"/> and every glyph <paramref name="yShift"/> lower.
        /// </summary>
        public string ToFnt(int advanceGrowth, int textureHeight, int yShift)
        {
            var lines = _lines.Select(line =>
            {
                if (line.StartsWith("char "))
                {
                    Glyph g = Glyphs[Values(line)["id"]];
                    return $"char id={g.Id} x={g.X} y={g.Y + yShift} width={g.Width} height={g.Height} " +
                        $"xoffset={g.XOffset} yoffset={g.YOffset} xadvance={g.XAdvance + advanceGrowth} page=0 chnl=15";
                }
                if (line.StartsWith("common "))
                {
                    line = ScaleW().Replace(line, $"scaleW={Width}");
                    return ScaleH().Replace(line, $"scaleH={textureHeight}");
                }
                return line;
            });
            return string.Join('\n', lines);
        }

        private static Dictionary<string, int> Values(string line) =>
            NumberValue().Matches(line).ToDictionary(match => match.Groups[1].Value, match => int.Parse(match.Groups[2].Value));

        [GeneratedRegex(@"(\w+)=(-?\d+)\b")]
        private static partial Regex NumberValue();

        [GeneratedRegex(@"scaleW=\d+")]
        private static partial Regex ScaleW();

        [GeneratedRegex(@"scaleH=\d+")]
        private static partial Regex ScaleH();
    }
}
