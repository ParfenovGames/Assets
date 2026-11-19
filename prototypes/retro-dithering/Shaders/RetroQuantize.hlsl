#ifndef RETRO_QUANTIZE_INCLUDED
#define RETRO_QUANTIZE_INCLUDED

// Shared colour-depth reduction used by Retro Lit (per object) and Retro Color Depth (post effect).
// Quantizes each channel to N levels in sRGB space, optionally with a 4x4 ordered dither.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Bayer 4x4 threshold in [-0.5, 0.5).
float RetroBayer4(uint2 p)
{
    uint x = p.x & 3u;
    uint y = p.y & 3u;
    uint xy = x ^ y;
    uint v = ((xy & 1u) << 3) | ((y & 1u) << 2) | (xy & 2u) | ((y & 2u) >> 1);
    return (v + 0.5) / 16.0 - 0.5;
}

// color: linear (or gamma in Gamma projects). levels: values per channel (8 = 3 bits).
// ditherStrength: 0 = hard bands, 1 = classic ordered dither. pixel: screen position in pixels.
float3 RetroQuantize(float3 color, float levels, float ditherStrength, float2 pixel)
{
    float3 c = saturate(color);
    #if !UNITY_COLORSPACE_GAMMA
    c = LinearToSRGB(c);
    #endif

    float steps = max(2.0, levels) - 1.0;
    float threshold = RetroBayer4((uint2)pixel) * ditherStrength;
    c = saturate(floor(c * steps + 0.5 + threshold) / steps);

    #if !UNITY_COLORSPACE_GAMMA
    c = SRGBToLinear(c);
    #endif
    return c;
}

#endif
