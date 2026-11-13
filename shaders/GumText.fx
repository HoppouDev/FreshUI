// Text glyphs drawn from a signed distance field (alpha: 0.5 at a glyph's edge, higher inside),
// their edges smoothed over one screen pixel from the field's screen-space derivatives, so text is
// sharp at any size. Edge moves the drawn edge along the field, for procedural weights; an outline
// is a second edge further out, a gradient colours each line from the capitals' top to the
// baseline, and glyphs in the atlas's lower half (from ShadowStart) are a drop shadow: the outer
// edge, softened. Widths are in screen pixels. Outputs premultiplied alpha. SpriteBatch sets
// MatrixTransform (Aether's FNA patch's Texture.EffectEXT; see src/Gum/GumText.cs).
//
// Compile with the Windows SDK's fxc (D3D9 effects; under Wine, with the SDK's d3dcompiler_47, as
// vkd3d can't yet write fx_2_0 passes):
//   fxc /nologo /T fx_2_0 /Fo resources/gum/GumText.fxb shaders/GumText.fx
//   WINEDLLOVERRIDES=d3dcompiler_47=n wine "<Windows Kits>/10/bin/<version>/x64/fxc.exe" /nologo /T fx_2_0 \
//     /Fo "$(winepath -w resources/gum/GumText.fxb)" "$(winepath -w shaders/GumText.fx)"

sampler TextureSampler : register(s0);

float4x4 MatrixTransform;

// The field value drawn as the glyph's edge: below 0.5 is bolder, above is thinner.
float Edge = 0.5;

// The field's change per atlas pixel: 1 / (2 * spread).
float FieldPerTexel = 1.0 / 32.0;

float2 TextureSize = float2(1024, 1024);

// Glyphs sit in rows of RowHeight atlas pixels at their place in the line, RowTop being the line y
// (pixels from the line's top) of a row's first pixel, so a pixel's height in its line is known.
float RowHeight = 64;
float RowTop = 0;

// The gradient runs from GradientTop at line y GradientStart to GradientBottom at GradientEnd.
float GradientStart = 0;
float GradientEnd = 64;
float4 GradientTop = float4(1, 1, 1, 1);
float4 GradientBottom = float4(1, 1, 1, 1);

float OutlineWidth = 0;
float4 OutlineColor = float4(0, 0, 0, 0);

// Atlas y from which glyphs are shadows, and the shadow's blur radius.
float ShadowStart = 100000;
float ShadowBlur = 0;

void SdfVertexShader(inout float4 color : COLOR0, inout float2 texCoord : TEXCOORD0, inout float4 position : POSITION0)
{
    position = mul(position, MatrixTransform);
}

float4 SdfPixelShader(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float distance = tex2D(TextureSampler, texCoord).a;
    float2 texel = texCoord * TextureSize;
    // The field's change across one screen pixel, from the atlas pixels one spans.
    float pixel = max(length(float2(ddx(texel.x), ddy(texel.x))) * FieldPerTexel, 1.0 / 4096.0);
    float outerEdge = Edge - OutlineWidth * pixel;

    if (texel.y >= ShadowStart)
    {
        return color * saturate((distance - outerEdge) / (max(2.0 * ShadowBlur, 1.0) * pixel) + 0.5);
    }

    float fill = saturate((distance - Edge) / pixel + 0.5);
    float outer = saturate((distance - outerEdge) / pixel + 0.5);
    float lineY = fmod(texel.y, RowHeight) + RowTop;
    float4 fillColor = color * lerp(GradientTop, GradientBottom,
        saturate((lineY - GradientStart) / (GradientEnd - GradientStart)));
    // The fill over the outline, both faded with the text.
    return fillColor * fill + OutlineColor * color.a * outer * (1.0 - fillColor.a * fill);
}

technique SdfText
{
    pass
    {
        VertexShader = compile vs_3_0 SdfVertexShader();
        PixelShader = compile ps_3_0 SdfPixelShader();
    }
}
