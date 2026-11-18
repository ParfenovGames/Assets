using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ParfenovGames.RetroDithering
{
    public enum BayerMatrixSize
    {
        Bayer2x2 = 2,
        Bayer4x4 = 4,
        Bayer8x8 = 8,
    }

    [Serializable]
    public sealed class BayerMatrixSizeParameter : VolumeParameter<BayerMatrixSize>
    {
        public BayerMatrixSizeParameter(BayerMatrixSize value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Volume settings for the Retro Dithering effect. Needs RetroDitheringRendererFeature on the URP renderer.
    /// Defaults are tuned for a dark, warm, 3-bit-per-channel horror look.
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("Post-processing Custom/Retro Dithering")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class RetroDitheringVolume : VolumeComponent
    {
        [Header("General")]
        [Tooltip("Blend between the original image (0) and the effect (1). The effect is off at 0.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Size of one retro pixel in screen pixels.")]
        public ClampedIntParameter pixelSize = new ClampedIntParameter(2, 1, 8);

        [Header("Grading (before quantization)")]
        [Tooltip("Exposure in EV, applied in linear space.")]
        public FloatParameter exposure = new FloatParameter(0f);

        public ClampedFloatParameter contrast = new ClampedFloatParameter(1.1f, 0f, 2f);

        public ClampedFloatParameter saturation = new ClampedFloatParameter(0.9f, 0f, 2f);

        [Tooltip("Values below this level become black. Crushes the shadows.")]
        public ClampedFloatParameter blackLevel = new ClampedFloatParameter(0.04f, 0f, 0.5f);

        [Tooltip("Colour multiplier in sRGB space.")]
        public ColorParameter tint = new ColorParameter(new Color(1f, 0.85f, 0.8f), false, false, true);

        [Header("Dithering")]
        [Tooltip("Levels per channel when no palette LUT is set. 8 = 3 bits per channel.")]
        public ClampedIntParameter colorLevels = new ClampedIntParameter(8, 2, 32);

        [Tooltip("0 = plain posterize, 1 = classic ordered dither, >1 = noisier.")]
        public ClampedFloatParameter ditherStrength = new ClampedFloatParameter(1f, 0f, 2f);

        public BayerMatrixSizeParameter bayerMatrix = new BayerMatrixSizeParameter(BayerMatrixSize.Bayer4x4);

        [Header("Palette (optional)")]
        [Tooltip("3D LUT baked from a palette image (Assets > Retro Dithering > Bake Palette LUT). Empty = per-channel levels.")]
        public Texture3DParameter paletteLut = new Texture3DParameter(null);

        [Tooltip("Dither amplitude in palette mode, in sRGB units. Roughly the distance between neighbouring palette colours.")]
        public ClampedFloatParameter paletteDitherSpread = new ClampedFloatParameter(0.12f, 0f, 0.5f);

        [Header("Extras")]
        public ClampedFloatParameter grain = new ClampedFloatParameter(0.03f, 0f, 0.3f);

        public BoolParameter animateGrain = new BoolParameter(true);

        public ClampedFloatParameter vignette = new ClampedFloatParameter(0.4f, 0f, 1f);

        public bool IsActive() => active && intensity.value > 0f;
    }
}
