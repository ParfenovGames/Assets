// Retro Color Depth: full-screen colour-depth reduction (N levels per channel in sRGB).
// Same maths as Retro Lit, applied to the whole frame instead of per object.
Shader "Hidden/ParfenovGames/RetroColorDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "RetroColorDepth"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "RetroQuantize.hlsl"

            float4 _RetroColorDepthParams; // levels, dither strength, exposure multiplier, intensity

            SAMPLER(sampler_RetroPointClamp);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_RetroPointClamp, input.texcoord).rgb;
                float3 c = RetroQuantize(original * _RetroColorDepthParams.z,
                                         _RetroColorDepthParams.x,
                                         _RetroColorDepthParams.y,
                                         input.positionCS.xy);

                return half4(lerp(original, c, _RetroColorDepthParams.w), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
