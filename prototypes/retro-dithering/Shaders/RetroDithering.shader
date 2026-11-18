// Retro Dithering — full-screen post effect for URP (Unity 6, Render Graph).
// Order: pixelate -> exposure -> to sRGB -> grading -> vignette/grain -> ordered dither + quantize -> to linear.
Shader "Hidden/ParfenovGames/RetroDithering"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "RetroDithering"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local_fragment _ _RETRO_PALETTE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _DitherSourceSize;   // width, height, 1/width, 1/height
            float  _DitherPixelSize;    // size of one "retro" pixel in screen pixels
            float  _DitherIntensity;    // blend with the original image
            float4 _DitherGrading;      // exposure multiplier, contrast, saturation, black level
            float4 _DitherTint;         // multiplier in sRGB space
            float4 _DitherParams;       // levels per channel, dither strength, bayer size (2/4/8), palette spread
            float4 _DitherExtras;       // grain, animate grain (0/1), vignette, unused
            float  _DitherLutSize;

            TEXTURE3D(_DitherPaletteLut);
            SAMPLER(sampler_DitherPointClamp);
            SAMPLER(sampler_DitherLinearClamp);

            // Ordered dither threshold in [-0.5, 0.5). Bayer matrices built by bit interleaving.
            float BayerThreshold(uint2 p)
            {
                uint x = p.x;
                uint y = p.y;

                if (_DitherParams.z > 6.0)
                {
                    x &= 7u; y &= 7u;
                    uint xy = x ^ y;
                    uint v = ((xy & 1u) << 5) | ((y & 1u) << 4) | ((xy & 2u) << 2) |
                             ((y & 2u) << 1)  | ((xy & 4u) >> 1) | ((y & 4u) >> 2);
                    return (v + 0.5) / 64.0 - 0.5;
                }

                if (_DitherParams.z > 3.0)
                {
                    x &= 3u; y &= 3u;
                    uint xy = x ^ y;
                    uint v = ((xy & 1u) << 3) | ((y & 1u) << 2) | (xy & 2u) | ((y & 2u) >> 1);
                    return (v + 0.5) / 16.0 - 0.5;
                }

                x &= 1u; y &= 1u;
                uint v2 = (((x ^ y) & 1u) << 1) | (y & 1u);
                return (v2 + 0.5) / 4.0 - 0.5;
            }

            float Hash12(float2 p)
            {
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // 1. Pixelate: snap to a low-resolution grid. Sampling the cell centre with a bilinear
                //    sampler gives a 2x2 box average for even pixel sizes.
                float  pixelSize = max(1.0, _DitherPixelSize);
                float2 cell      = floor(uv * _DitherSourceSize.xy / pixelSize);
                float2 cellUV    = (cell + 0.5) * pixelSize * _DitherSourceSize.zw;

                float3 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_DitherPointClamp, uv).rgb;
                float3 c        = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_DitherLinearClamp, cellUV).rgb;

                // 2. Exposure in linear space, then move to sRGB: quantizing in linear wastes levels on highlights.
                c = saturate(c * _DitherGrading.x);
                #if !UNITY_COLORSPACE_GAMMA
                c = LinearToSRGB(c);
                #endif

                // 3. Grading before quantization.
                c *= _DitherTint.rgb;
                float luma = dot(c, float3(0.2126, 0.7152, 0.0722));
                c = lerp(luma.xxx, c, _DitherGrading.z);
                c = (c - 0.5) * _DitherGrading.y + 0.5;
                c = saturate((c - _DitherGrading.w) / max(1e-4, 1.0 - _DitherGrading.w));

                // 4. Vignette and grain go before quantization so they get dithered too.
                float2 d = (cellUV - 0.5) * float2(_DitherSourceSize.x * _DitherSourceSize.w, 1.0);
                float vignette = 1.0 - smoothstep(0.3, 1.0, length(d));
                c *= lerp(1.0, vignette, _DitherExtras.z);

                float seed = _DitherExtras.y > 0.5 ? floor(_Time.y * 24.0) : 0.0;
                c = saturate(c + (Hash12(cell + seed * 17.13) - 0.5) * _DitherExtras.x);

                // 5. Ordered dither + quantization, anchored to the low-res grid so the pattern stays crisp.
                float threshold = BayerThreshold((uint2)cell);

                #if defined(_RETRO_PALETTE)
                    c = saturate(c + threshold * _DitherParams.y * _DitherParams.w);
                    float lutSize = max(2.0, _DitherLutSize);
                    float3 lutUV  = (c * (lutSize - 1.0) + 0.5) / lutSize;
                    c = SAMPLE_TEXTURE3D_LOD(_DitherPaletteLut, sampler_DitherPointClamp, lutUV, 0).rgb;
                #else
                    float steps = max(2.0, _DitherParams.x) - 1.0;
                    c = saturate(floor(c * steps + 0.5 + threshold * _DitherParams.y) / steps);
                #endif

                #if !UNITY_COLORSPACE_GAMMA
                c = SRGBToLinear(c);
                #endif

                return half4(lerp(original, c, _DitherIntensity), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
