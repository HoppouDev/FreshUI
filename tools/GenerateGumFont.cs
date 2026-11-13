#:package KernSmith@0.27.0
#:package KernSmith.Rasterizers.StbTrueType@0.27.0
#:property PublishAot=false

// Bakes a font into a signed distance field bitmap font (.fnt + .png) for Gum.
// All Gum text draws from this one atlas, scaled to its size and shaded by shaders/GumText.fx
// (see src/Gum/GumText.cs), so it is sharp at any size.
//
//   dotnet run --file tools/GenerateGumFont.cs -- --font <.ttf or .otf>
//
// <font> (Andy Bold: ANDYB.ttf) -> resources/gum/TextSdf.fnt + TextSdf_0.png, and TextSdf.json: the
// atlas's pixels per em, its field's spread and the font's design weight, which src/Gum/GumText.cs reads.

using System.Text.Json;
using KernSmith;

// Pixels per em the field is rendered at.
const int AtlasSize = 64;
// Pixels from a glyph's edge to the end of its field (a quarter em), bounding how far outlines,
// shadow blur and weights reach.
const int Spread = 16;

string repoRoot = FindRepoRoot();
string fontsDir = Path.Combine(repoRoot, "resources", "gum");

string? font = GetArg("--font") ?? FindFont(fontsDir);
if (font == null || !File.Exists(font))
{
    Console.Error.WriteLine($"No font to bake. Put one .ttf or .otf in {fontsDir}, or pass --font <path>.");
    return 1;
}

var info = BmFont.ReadFontInfo(font);
var result = BmFont.Generate(font, new FontGeneratorOptions
{
    Size = AtlasSize,
    // Every character the font has, from the space up (control characters have no glyphs).
    Characters = CharacterSet.FromChars(info.AvailableCodepoints.Where(c => c >= 0x20)),
    Kerning = true,
    Sdf = true,
    SdfSpread = Spread,
    // FreeType's distance fields can break where strokes join, leaving pits below the edge that
    // draw as holes in the glyph. StbTrueType's are whole and otherwise the same.
    Backend = RasterizerBackend.StbTrueType
});

string outBase = Path.Combine(fontsDir, "TextSdf");
result.ToFile(outBase);
File.WriteAllText(outBase + ".json", JsonSerializer.Serialize(new
{
    size = AtlasSize,
    spread = Spread,
    // The CSS-style weight the font is drawn at as designed (400 regular, 700 bold).
    weight = info.Os2?.WeightClass is > 0 and var weightClass ? weightClass : 400
}));

Console.WriteLine($"Baked {Path.GetFileName(font)} into {Path.GetRelativePath(repoRoot, outBase)}.fnt ({result.Pages.Count} page(s))");
foreach (var failed in result.FailedCodepoints)
{
    Console.Error.WriteLine($"Failed: {failed}");
}

return 0;

static string? FindFont(string dir)
{
    string[] fonts = Directory.Exists(dir)
        ? Directory.GetFiles(dir).Where(f => Path.GetExtension(f).ToLowerInvariant() is ".ttf" or ".otf").ToArray()
        : [];
    if (fonts.Length > 1)
    {
        Console.Error.WriteLine($"Several fonts in {dir}; choose one with --font.");
        return null;
    }
    return fonts.FirstOrDefault();
}

static string? GetArg(string name)
{
    string[] args = Environment.GetCommandLineArgs();
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRepoRoot()
{
    string? scriptDir = AppContext.GetData("EntryPointFileDirectoryPath") as string;
    for (string? dir = scriptDir ?? Directory.GetCurrentDirectory(); dir != null; dir = Path.GetDirectoryName(dir))
    {
        if (File.Exists(Path.Combine(dir, "FreshUI.csproj")))
        {
            return dir;
        }
    }
    throw new InvalidOperationException("Could not find FreshUI.csproj above the script.");
}
