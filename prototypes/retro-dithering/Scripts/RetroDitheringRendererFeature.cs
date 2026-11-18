using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ParfenovGames.RetroDithering
{
    /// <summary>
    /// Adds the Retro Dithering full-screen pass. Settings come from RetroDitheringVolume.
    /// </summary>
    [DisallowMultipleRendererFeature("Retro Dithering")]
    public sealed class RetroDitheringRendererFeature : ScriptableRendererFeature
    {
        [SerializeField, Tooltip("Hidden/ParfenovGames/RetroDithering. Filled in automatically.")]
        Shader shader;

        [SerializeField, Tooltip("After Rendering Post Processing quantizes the final tonemapped image (recommended).")]
        RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        [SerializeField]
        bool applyInSceneView = true;

        Material m_Material;
        RetroDitheringPass m_Pass;

        public override void Create()
        {
            if (shader == null)
                shader = Shader.Find(RetroDitheringPass.ShaderName);

            if (shader == null)
            {
                Debug.LogWarning($"Retro Dithering: shader '{RetroDitheringPass.ShaderName}' not found.");
                return;
            }

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass = new RetroDitheringPass(m_Material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null)
                return;

            var settings = VolumeManager.instance.stack?.GetComponent<RetroDitheringVolume>();
            if (settings == null || !settings.IsActive())
                return;

            m_Pass.renderPassEvent = injectionPoint;
            m_Pass.applyInSceneView = applyInSceneView;
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
            m_Material = null;
            m_Pass = null;
        }
    }

    sealed class RetroDitheringPass : ScriptableRenderPass
    {
        public const string ShaderName = "Hidden/ParfenovGames/RetroDithering";
        const string k_PaletteKeyword = "_RETRO_PALETTE";

        static readonly int s_SourceSize = Shader.PropertyToID("_DitherSourceSize");
        static readonly int s_PixelSize = Shader.PropertyToID("_DitherPixelSize");
        static readonly int s_Intensity = Shader.PropertyToID("_DitherIntensity");
        static readonly int s_Grading = Shader.PropertyToID("_DitherGrading");
        static readonly int s_Tint = Shader.PropertyToID("_DitherTint");
        static readonly int s_Params = Shader.PropertyToID("_DitherParams");
        static readonly int s_Extras = Shader.PropertyToID("_DitherExtras");
        static readonly int s_Lut = Shader.PropertyToID("_DitherPaletteLut");
        static readonly int s_LutSize = Shader.PropertyToID("_DitherLutSize");

        readonly Material m_Material;
        public bool applyInSceneView = true;

        public RetroDitheringPass(Material material)
        {
            m_Material = material;
            profilingSampler = new ProfilingSampler("Retro Dithering");
            // Reading and swapping the camera colour needs an intermediate texture, not the back buffer.
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<RetroDitheringVolume>();
            if (settings == null || !settings.IsActive())
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var cameraType = cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
                return;
            if (cameraType == CameraType.SceneView && !applyInSceneView)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc desc = renderGraph.GetTextureDesc(source);

            int width = desc.width;
            int height = desc.height;
            if (desc.sizeMode != TextureSizeMode.Explicit || width <= 0 || height <= 0)
            {
                width = cameraData.cameraTargetDescriptor.width;
                height = cameraData.cameraTargetDescriptor.height;
            }

            UpdateMaterial(settings, width, height);

            desc.name = "_RetroDitheringColor";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, m_Material, 0);
            renderGraph.AddBlitPass(blitParams, passName: "Retro Dithering");

            resourceData.cameraColor = destination;
        }

        void UpdateMaterial(RetroDitheringVolume s, int width, int height)
        {
            var m = m_Material;

            m.SetVector(s_SourceSize, new Vector4(width, height, 1f / width, 1f / height));
            m.SetFloat(s_PixelSize, s.pixelSize.value);
            m.SetFloat(s_Intensity, s.intensity.value);
            m.SetVector(s_Grading, new Vector4(
                Mathf.Pow(2f, s.exposure.value),
                s.contrast.value,
                s.saturation.value,
                s.blackLevel.value));

            // SetVector, not SetColor: the tint is applied in sRGB space and must not be converted to linear.
            Color tint = s.tint.value;
            m.SetVector(s_Tint, new Vector4(tint.r, tint.g, tint.b, 1f));

            m.SetVector(s_Params, new Vector4(
                s.colorLevels.value,
                s.ditherStrength.value,
                (float)s.bayerMatrix.value,
                s.paletteDitherSpread.value));

            m.SetVector(s_Extras, new Vector4(
                s.grain.value,
                s.animateGrain.value ? 1f : 0f,
                s.vignette.value,
                0f));

            var lut = s.paletteLut.value as Texture3D;
            bool usePalette = lut != null;
            CoreUtils.SetKeyword(m, k_PaletteKeyword, usePalette);
            if (usePalette)
            {
                m.SetTexture(s_Lut, lut);
                m.SetFloat(s_LutSize, lut.width);
            }
        }
    }
}
