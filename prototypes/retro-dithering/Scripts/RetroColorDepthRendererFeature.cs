using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ParfenovGames.RetroDithering
{
    /// <summary>
    /// Adds the Retro Color Depth full-screen pass. Settings come from RetroColorDepthVolume.
    /// </summary>
    [DisallowMultipleRendererFeature("Retro Color Depth")]
    public sealed class RetroColorDepthRendererFeature : ScriptableRendererFeature
    {
        [SerializeField, Tooltip("Hidden/ParfenovGames/RetroColorDepth. Filled in automatically.")]
        Shader shader;

        [SerializeField, Tooltip("After Rendering Post Processing quantizes the final image (recommended).")]
        RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        [SerializeField]
        bool applyInSceneView = true;

        Material m_Material;
        RetroColorDepthPass m_Pass;

        public override void Create()
        {
            if (shader == null)
                shader = Shader.Find(RetroColorDepthPass.ShaderName);

            if (shader == null)
            {
                Debug.LogWarning($"Retro Color Depth: shader '{RetroColorDepthPass.ShaderName}' not found.");
                return;
            }

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Pass = new RetroColorDepthPass(m_Material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null)
                return;

            var settings = VolumeManager.instance.stack?.GetComponent<RetroColorDepthVolume>();
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

    sealed class RetroColorDepthPass : ScriptableRenderPass
    {
        public const string ShaderName = "Hidden/ParfenovGames/RetroColorDepth";

        static readonly int s_Params = Shader.PropertyToID("_RetroColorDepthParams");

        readonly Material m_Material;
        public bool applyInSceneView = true;

        public RetroColorDepthPass(Material material)
        {
            m_Material = material;
            profilingSampler = new ProfilingSampler("Retro Color Depth");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<RetroColorDepthVolume>();
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

            m_Material.SetVector(s_Params, new Vector4(
                settings.colorLevels.value,
                settings.ditherStrength.value,
                Mathf.Pow(2f, settings.exposure.value),
                settings.intensity.value));

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = "_RetroColorDepthColor";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, m_Material, 0);
            renderGraph.AddBlitPass(blitParams, passName: "Retro Color Depth");

            resourceData.cameraColor = destination;
        }
    }
}
