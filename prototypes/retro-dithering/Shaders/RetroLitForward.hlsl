#ifndef RETRO_LIT_FORWARD_INCLUDED
#define RETRO_LIT_FORWARD_INCLUDED

// Wraps URP's LitPassFragment: full URP lighting, fog, shadows, then colour-depth reduction.
// Include after LitInput.hlsl and LitForwardPass.hlsl.

#include "RetroQuantize.hlsl"

// Global settings, set by the RetroLitSettings component (Shader.SetGlobalFloat).
// Not material properties, so the shader stays SRP Batcher compatible.
float _RetroLitColorLevels;     // 0 = not set -> 8
float _RetroLitDitherStrength;  // 0 = hard bands, like the reference

void RetroLitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    float2 pixel = input.positionCS.xy;

#ifdef _WRITE_RENDERING_LAYERS
    LitPassFragment(input, outColor, outRenderingLayers);
#else
    LitPassFragment(input, outColor);
#endif

    float levels = _RetroLitColorLevels >= 2.0 ? _RetroLitColorLevels : 8.0;
    outColor.rgb = RetroQuantize(outColor.rgb, levels, _RetroLitDitherStrength, pixel);
}

#endif
