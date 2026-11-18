using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ParfenovGames.RetroDithering.Editor
{
    /// <summary>
    /// Bakes a palette image (any PNG/JPG, every unique colour is a palette entry) into a 32^3 Texture3D.
    /// Each LUT cell stores the nearest palette colour, measured in Oklab.
    /// Usage: select the palette image > Assets > Retro Dithering > Bake Palette LUT.
    /// </summary>
    static class RetroDitheringLutBaker
    {
        const int k_LutSize = 32;
        const int k_MaxPaletteColors = 256;
        const string k_MenuPath = "Assets/Retro Dithering/Bake Palette LUT";

        [MenuItem(k_MenuPath, true)]
        static bool ValidateBake() => Selection.activeObject is Texture2D;

        [MenuItem(k_MenuPath, false, 2000)]
        static void BakeSelected()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D texture)
                    Bake(texture);
            }
        }

        public static Texture3D Bake(Texture2D paletteTexture)
        {
            string palettePath = AssetDatabase.GetAssetPath(paletteTexture);
            List<Color32> palette = ReadPalette(palettePath);
            if (palette == null)
                return null;

            var paletteLab = new Vector3[palette.Count];
            for (int i = 0; i < palette.Count; i++)
                paletteLab[i] = SrgbToOklab(palette[i].r / 255f, palette[i].g / 255f, palette[i].b / 255f);

            var pixels = new Color32[k_LutSize * k_LutSize * k_LutSize];
            float inv = 1f / (k_LutSize - 1);

            for (int b = 0; b < k_LutSize; b++)
            for (int g = 0; g < k_LutSize; g++)
            for (int r = 0; r < k_LutSize; r++)
            {
                Vector3 lab = SrgbToOklab(r * inv, g * inv, b * inv);
                int best = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < paletteLab.Length; i++)
                {
                    float dist = (paletteLab[i] - lab).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = i;
                    }
                }
                pixels[r + g * k_LutSize + b * k_LutSize * k_LutSize] = palette[best];
            }

            // UNorm (not sRGB): the shader works in sRGB values and expects them back unchanged.
            var lut = new Texture3D(k_LutSize, k_LutSize, k_LutSize, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
            {
                name = Path.GetFileNameWithoutExtension(palettePath) + "_LUT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
            };
            lut.SetPixels32(pixels);
            lut.Apply(false, false);

            string lutPath = Path.ChangeExtension(palettePath, null) + "_LUT.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture3D>(lutPath);
            if (existing != null)
            {
                // Keep the GUID so volume profiles that reference the LUT stay linked.
                EditorUtility.CopySerialized(lut, existing);
                Object.DestroyImmediate(lut);
                lut = existing;
                EditorUtility.SetDirty(lut);
            }
            else
            {
                AssetDatabase.CreateAsset(lut, lutPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Retro Dithering: baked {palette.Count}-colour LUT to {lutPath}", lut);
            EditorGUIUtility.PingObject(lut);
            return lut;
        }

        // Reads the source file directly, so the texture's import settings (compression, size, Read/Write) don't matter.
        static List<Color32> ReadPalette(string assetPath)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!image.LoadImage(File.ReadAllBytes(assetPath)))
                {
                    Debug.LogError($"Retro Dithering: can't decode {assetPath}. Use PNG or JPG.");
                    return null;
                }

                var unique = new HashSet<int>();
                var palette = new List<Color32>();
                foreach (Color32 c in image.GetPixels32())
                {
                    if (c.a < 128)
                        continue;
                    int key = (c.r << 16) | (c.g << 8) | c.b;
                    if (unique.Add(key))
                        palette.Add(new Color32(c.r, c.g, c.b, 255));
                    if (palette.Count > k_MaxPaletteColors)
                    {
                        Debug.LogError($"Retro Dithering: {assetPath} has more than {k_MaxPaletteColors} colours. " +
                                       "Use a small palette image (e.g. 16x1 pixels).");
                        return null;
                    }
                }

                if (palette.Count == 0)
                {
                    Debug.LogError($"Retro Dithering: {assetPath} has no opaque pixels.");
                    return null;
                }
                return palette;
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }

        static float SrgbToLinear(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);

        static Vector3 SrgbToOklab(float sr, float sg, float sb)
        {
            float r = SrgbToLinear(sr), g = SrgbToLinear(sg), b = SrgbToLinear(sb);

            float l = Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
            float m = Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
            float s = Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);

            return new Vector3(
                0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
                1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
                0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
        }

        static float Cbrt(float x) => x < 0f ? -Mathf.Pow(-x, 1f / 3f) : Mathf.Pow(x, 1f / 3f);
    }
}
