// FreshUI's text: glyphs drawn from a signed distance field (red: 0.5 at a glyph's edge, higher
// inside), their edges smoothed over one screen pixel from the field's screen-space derivatives,
// so text is sharp at any size. SpriteBatch sets MatrixTransform (Aether's Texture.EffectEXT).
//
// Compile with the Windows SDK's fxc (D3D9 effects; under Wine, with its native d3dcompiler_47):
//   fxc /nologo /T fx_2_0 /Fo resources/shaders/SdfText.fxb shaders/SdfText.fx

sampler TextureSampler : register(s0);

float4x4 MatrixTransform;

void SdfVertexShader(inout float4 color : COLOR0, inout float2 texCoord : TEXCOORD0, inout float4 position : POSITION0)
{
    position = mul(position, MatrixTransform);
}

float4 SdfPixelShader(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float distance = tex2D(TextureSampler, texCoord).r;
    float width = max(length(float2(ddx(distance), ddy(distance))), 1.0 / 1024.0);
    return color * saturate((distance - 0.5) / width + 0.5);
}

technique SdfText
{
    pass
    {
        VertexShader = compile vs_3_0 SdfVertexShader();
        PixelShader = compile ps_3_0 SdfPixelShader();
    }
}
