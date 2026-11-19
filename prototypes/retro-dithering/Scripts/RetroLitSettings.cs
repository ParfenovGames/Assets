using UnityEngine;

namespace ParfenovGames.RetroDithering
{
    /// <summary>
    /// Global settings for every material that uses the "ParfenovGames/Retro Lit" shader.
    /// Put one in the scene. Without it Retro Lit uses 8 levels per channel and no dither.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Rendering/Retro Lit Settings")]
    public sealed class RetroLitSettings : MonoBehaviour
    {
        static readonly int s_ColorLevels = Shader.PropertyToID("_RetroLitColorLevels");
        static readonly int s_DitherStrength = Shader.PropertyToID("_RetroLitDitherStrength");

        [Tooltip("Values per channel. 8 = 3 bits per channel.")]
        [Range(2, 32)] public int colorLevels = 8;

        [Tooltip("0 = hard bands (like the reference), 1 = ordered 4x4 dither.")]
        [Range(0f, 1f)] public float ditherStrength = 0f;

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Update() => Apply();

        // Back to defaults (8 levels, no dither) when the component is removed or disabled.
        void OnDisable()
        {
            Shader.SetGlobalFloat(s_ColorLevels, 0f);
            Shader.SetGlobalFloat(s_DitherStrength, 0f);
        }

        void Apply()
        {
            Shader.SetGlobalFloat(s_ColorLevels, colorLevels);
            Shader.SetGlobalFloat(s_DitherStrength, ditherStrength);
        }
    }
}
