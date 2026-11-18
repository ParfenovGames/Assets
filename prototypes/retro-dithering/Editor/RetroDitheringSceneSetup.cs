using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ParfenovGames.RetroDithering.Editor
{
    /// <summary>
    /// Tools > Retro Dithering > Setup Active Scene:
    /// 1. adds RetroDitheringRendererFeature to every URP renderer used by the quality levels;
    /// 2. creates a global Volume with a Retro Dithering profile in the active scene;
    /// 3. turns off anti-aliasing on the main camera.
    /// Everything except the renderer change supports Undo.
    /// </summary>
    static class RetroDitheringSceneSetup
    {
        const string k_VolumeName = "Retro Dithering Volume";

        [MenuItem("Tools/Retro Dithering/Setup Active Scene")]
        static void SetupActiveScene()
        {
            int addedFeatures = AddFeatureToAllRenderers();

            Scene scene = SceneManager.GetActiveScene();
            Volume volume = FindOrCreateVolume(scene);

            bool cameraChanged = DisableCameraAntialiasing();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = volume.gameObject;

            Debug.Log($"Retro Dithering: renderer features added: {addedFeatures}, " +
                      $"volume '{volume.name}' in scene '{scene.name}', " +
                      $"camera anti-aliasing {(cameraChanged ? "turned off" : "unchanged")}. Save the scene to keep it.");
        }

        static int AddFeatureToAllRenderers()
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
                if (AddFeature(data))
                    added++;
            }
            AssetDatabase.SaveAssets();
            return added;
        }

        // Mirrors what the URP renderer inspector does on "Add Renderer Feature".
        static bool AddFeature(ScriptableRendererData data)
        {
            foreach (var existing in data.rendererFeatures)
            {
                if (existing is RetroDitheringRendererFeature)
                    return false;
            }

            var feature = ScriptableObject.CreateInstance<RetroDitheringRendererFeature>();
            feature.name = "RetroDithering";
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
            Debug.Log($"Retro Dithering: added renderer feature to {AssetDatabase.GetAssetPath(data)}", data);
            return true;
        }

        static Volume FindOrCreateVolume(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var existing in root.GetComponentsInChildren<Volume>(true))
                {
                    if (existing.isGlobal && existing.sharedProfile != null &&
                        existing.sharedProfile.TryGet(out RetroDitheringVolume _))
                        return existing;
                }
            }

            var go = new GameObject(k_VolumeName);
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Create Retro Dithering Volume");

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = CreateProfile(scene);
            return volume;
        }

        static VolumeProfile CreateProfile(Scene scene)
        {
            string folder = string.IsNullOrEmpty(scene.path)
                ? "Assets"
                : Path.GetDirectoryName(scene.path).Replace('\\', '/');
            string sceneName = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{sceneName}_RetroDithering.asset");

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            // Every parameter gets its override enabled, so all defaults show up ready to tweak.
            var settings = profile.Add<RetroDitheringVolume>(true);
            settings.name = nameof(RetroDitheringVolume);
            settings.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            settings.intensity.value = 1f;
            settings.paletteLut.overrideState = false;
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
    }
}
