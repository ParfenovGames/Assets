using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ParfenovGames.RetroDithering
{
    /// <summary>
    /// Volume settings for Retro Color Depth: plain per-channel colour-depth reduction of the whole frame.
    /// Needs RetroColorDepthRendererFeature on the URP renderer.
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("Post-processing Custom/Retro Color Depth")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class RetroColorDepthVolume : VolumeComponent
    {
        [Tooltip("Blend between the original image (0) and the effect (1). The effect is off at 0.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Values per channel. 8 = 3 bits per channel.")]
        public ClampedIntParameter colorLevels = new ClampedIntParameter(8, 2, 32);

        [Tooltip("0 = hard bands (like the reference), 1 = ordered 4x4 dither.")]
        public ClampedFloatParameter ditherStrength = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Exposure in EV, applied before quantization.")]
        public FloatParameter exposure = new FloatParameter(0f);

        public bool IsActive() => active && intensity.value > 0f;
    }
}
