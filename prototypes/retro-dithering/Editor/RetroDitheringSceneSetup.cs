using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ParfenovGames.RetroDithering.Editor
{
    /// <summary>
    /// Tools > Retro Dithering > Setup Active Scene > ...
    ///   Retro Dithering (Post)  - feature + global volume with the dithering effect;
    ///   Retro Color Depth (Post) - feature + global volume with plain colour-depth reduction;
    ///   Retro Lit (Per Object)   - switches URP Lit / Complex Lit materials in the scene to Retro Lit
    ///                              and adds a RetroLitSettings object.
    /// Tools > Retro Dithering > Revert Scene Materials To URP Lit - undoes the material switch.
    /// </summary>
    static class RetroDitheringSceneSetup
    {
        const string k_Menu = "Tools/Retro Dithering/";
        const string k_RetroLitShader = "ParfenovGames/Retro Lit";
        const string k_OriginalShaderTag = "RetroLitOriginalShader";
        static readonly string[] k_ConvertibleShaders =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Complex Lit",
        };

        // ---------------------------------------------------------------- post effects

        [MenuItem(k_Menu + "Setup Active Scene/Retro Dithering (Post)", false, 10)]
        static void SetupDithering()
        {
            SetupPostEffect<RetroDitheringRendererFeature, RetroDitheringVolume>(
                "RetroDithering", "Retro Dithering Volume",
                settings =>
                {
                    settings.intensity.value = 1f;
                    settings.paletteLut.overrideState = false;
                });
        }

        [MenuItem(k_Menu + "Setup Active Scene/Retro Color Depth (Post)", false, 11)]
        static void SetupColorDepth()
        {
            SetupPostEffect<RetroColorDepthRendererFeature, RetroColorDepthVolume>(
                "RetroColorDepth", "Retro Color Depth Volume",
                settings => settings.intensity.value = 1f);
        }

        static void SetupPostEffect<TFeature, TVolume>(string assetSuffix, string objectName, Action<TVolume> configure)
            where TFeature : ScriptableRendererFeature
            where TVolume : VolumeComponent
        {
            int addedFeatures = AddFeatureToAllRenderers<TFeature>();

            Scene scene = SceneManager.GetActiveScene();
            Volume volume = FindOrCreateVolume(scene, assetSuffix, objectName, configure);
            bool cameraChanged = DisableCameraAntialiasing();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = volume.gameObject;

            Debug.Log($"Retro Dithering: {typeof(TFeature).Name} added to {addedFeatures} renderer(s), " +
                      $"volume '{volume.name}' in scene '{scene.name}', " +
                      $"camera anti-aliasing {(cameraChanged ? "turned off" : "unchanged")}. Save the scene to keep it.");
        }

        static int AddFeatureToAllRenderers<TFeature>() where TFeature : ScriptableRendererFeature
        {
            var pipelines = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultAsset)
                pipelines.Add(defaultAsset);
            for (int i = 0; i < QualitySettings.count; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset qualityAsset)
                    pipelines.Add(qualityAsset);
            }

            if (pipelines.Count == 0)
            {
                Debug.LogWarning("Retro Dithering: no URP asset found in Graphics or Quality settings.");
                return 0;
            }

            var renderers = new HashSet<ScriptableRendererData>();
            foreach (var pipeline in pipelines)
            {
                var list = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableRendererData data)
                        renderers.Add(data);
                }
            }

            int added = 0;
            foreach (var data in renderers)
            {
                if (AddFeature<TFeature>(data))
                    added++;
            }
            AssetDatabase.SaveAssets();
            return added;
        }

        // Mirrors what the URP renderer inspector does on "Add Renderer Feature".
        static bool AddFeature<TFeature>(ScriptableRendererData data) where TFeature : ScriptableRendererFeature
        {
            foreach (var existing in data.rendererFeatures)
            {
                if (existing is TFeature)
                    return false;
            }

            var feature = ScriptableObject.CreateInstance<TFeature>();
            feature.name = typeof(TFeature).Name.Replace("RendererFeature", "");
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var so = new SerializedObject(data);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");

            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedProperties();

            data.SetDirty();
            EditorUtility.SetDirty(data);
            Debug.Log($"Retro Dithering: added {feature.name} to {AssetDatabase.GetAssetPath(data)}", data);
            return true;
        }

        static Volume FindOrCreateVolume<TVolume>(Scene scene, string assetSuffix, string objectName, Action<TVolume> configure)
            where TVolume : VolumeComponent
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var existing in root.GetComponentsInChildren<Volume>(true))
                {
                    if (existing.isGlobal && existing.sharedProfile != null && existing.sharedProfile.Has<TVolume>())
                        return existing;
                }
            }

            var go = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Create " + objectName);

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = CreateProfile(scene, assetSuffix, configure);
            return volume;
        }

        static VolumeProfile CreateProfile<TVolume>(Scene scene, string assetSuffix, Action<TVolume> configure)
            where TVolume : VolumeComponent
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{SceneFolder(scene)}/{SceneName(scene)}_{assetSuffix}.asset");

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            // Every parameter gets its override enabled, so all defaults show up ready to tweak.
            var settings = profile.Add<TVolume>(true);
            settings.name = typeof(TVolume).Name;
            settings.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            configure?.Invoke(settings);
            AssetDatabase.AddObjectToAsset(settings, profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log($"Retro Dithering: created profile {path}", profile);
            return profile;
        }

        static bool DisableCameraAntialiasing()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            var data = camera.GetUniversalAdditionalCameraData();
            if (data == null || data.antialiasing == AntialiasingMode.None)
                return false;

            Undo.RecordObject(data, "Disable Anti-aliasing");
            data.antialiasing = AntialiasingMode.None;
            return true;
        }

        // ---------------------------------------------------------------- Retro Lit (per object)

        [MenuItem(k_Menu + "Setup Active Scene/Retro Lit (Per Object)", false, 30)]
        static void SetupRetroLit()
        {
            Shader retroLit = Shader.Find(k_RetroLitShader);
            if (retroLit == null)
            {
                Debug.LogError($"Retro Dithering: shader '{k_RetroLitShader}' not found.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            var skipped = new HashSet<string>();
            int converted = 0;

            foreach (Material material in CollectSceneMaterials(scene))
            {
                if (material.shader == retroLit)
                    continue;

                if (Array.IndexOf(k_ConvertibleShaders, material.shader.name) < 0)
                {
                    skipped.Add($"{material.name} ({material.shader.name})");
                    continue;
                }

                if (!AssetDatabase.IsOpenForEdit(material))
                {
                    skipped.Add($"{material.name} (read-only)");
                    continue;
                }

                Undo.RecordObject(material, "Convert to Retro Lit");
                material.SetOverrideTag(k_OriginalShaderTag, material.shader.name);
                // Same property and keyword names as URP Lit, so the material keeps its look.
                material.shader = retroLit;
                EditorUtility.SetDirty(material);
                converted++;
            }

            AssetDatabase.SaveAssets();

            var settings = Object.FindAnyObjectByType<RetroLitSettings>(FindObjectsInactive.Include);
            if (settings == null)
            {
                var go = new GameObject("Retro Lit Settings");
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Create Retro Lit Settings");
                settings = go.AddComponent<RetroLitSettings>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            Selection.activeGameObject = settings.gameObject;

            Debug.Log($"Retro Dithering: {converted} material(s) switched to Retro Lit in scene '{scene.name}'.");
            if (skipped.Count > 0)
                Debug.Log("Retro Dithering: skipped (not URP Lit / Complex Lit or read-only):\n" + string.Join("\n", skipped));

            WarnAboutActivePostEffects(scene);
        }

        [MenuItem(k_Menu + "Revert Scene Materials To URP Lit", false, 31)]
        static void RevertRetroLit()
        {
            Scene scene = SceneManager.GetActiveScene();
            int reverted = 0;

            foreach (Material material in CollectSceneMaterials(scene))
            {
                if (material.shader == null || material.shader.name != k_RetroLitShader)
                    continue;

                string original = material.GetTag(k_OriginalShaderTag, false, k_ConvertibleShaders[0]);
                Shader shader = Shader.Find(original) ?? Shader.Find(k_ConvertibleShaders[0]);

                Undo.RecordObject(material, "Revert to URP Lit");
                material.shader = shader;
                material.SetOverrideTag(k_OriginalShaderTag, "");
                EditorUtility.SetDirty(material);
                reverted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Retro Dithering: {reverted} material(s) reverted in scene '{scene.name}'.");
        }

        static IEnumerable<Material> CollectSceneMaterials(Scene scene)
        {
            var seen = new HashSet<Material>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null && seen.Add(material))
                            yield return material;
                    }
                }
            }
        }

        // Per-object quantization plus a full-screen one on top is usually not what you want.
        static void WarnAboutActivePostEffects(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var volume in root.GetComponentsInChildren<Volume>(false))
                {
                    if (!volume.enabled || volume.sharedProfile == null)
                        continue;

                    bool dithering = volume.sharedProfile.TryGet(out RetroDitheringVolume d) && d.active && d.intensity.value > 0f;
                    bool colorDepth = volume.sharedProfile.TryGet(out RetroColorDepthVolume c) && c.active && c.intensity.value > 0f;
                    if (dithering || colorDepth)
                        Debug.LogWarning($"Retro Dithering: volume '{volume.name}' also applies a full-screen retro effect. " +
                                         "Disable it to see Retro Lit on its own.", volume);
                }
            }
        }

        static string SceneFolder(Scene scene) =>
            string.IsNullOrEmpty(scene.path) ? "Assets" : Path.GetDirectoryName(scene.path).Replace('\\', '/');

        static string SceneName(Scene scene) =>
            string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
    }
}
