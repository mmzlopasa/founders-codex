#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.iOS;
using UnityEngine;

namespace Founders.EditorTools
{
    /// <summary>
    /// Generates original settlement branding during the remote Unity build so
    /// no project art needs to be stored on the user's computer.
    /// </summary>
    internal static class BrandingAssetBuilder
    {
        private const string GeneratedFolder = "Assets/Founders/Generated";
        private const string IconPath = GeneratedFolder + "/Founders_App_Icon.png";
        private const string LoadingPath = GeneratedFolder + "/Prehistoric_Loading_Background.png";

        internal static void GenerateAssets()
        {
            Directory.CreateDirectory(GeneratedFolder);
            SavePng(IconPath, BuildIcon(1024, 1024));
            SavePng(LoadingPath, BuildLoadingBackground(1920, 1080));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureImporter(IconPath, TextureImporterType.Default);
            ConfigureImporter(LoadingPath, TextureImporterType.Sprite);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(LoadingPath, ImportAssetOptions.ForceUpdate);
            ConfigureIosIcons();
        }

        private static void ConfigureImporter(string path, TextureImporterType type)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = type;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void ConfigureIosIcons()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
                return;

            var kind = iOSPlatformIconKind.Application;
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
            for (var i = 0; i < icons.Length; i++)
            {
                var layers = new Texture2D[icons[i].maxLayerCount];
                for (var layer = 0; layer < layers.Length; layer++)
                    layers[layer] = icon;
                icons[i].SetTextures(layers);
            }

            PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
        }

        private static void SavePng(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static Texture2D BuildIcon(int width, int height)
        {
            var pixels = Gradient(width, height, new Color32(31, 49, 54, 255), new Color32(89, 105, 68, 255));
            AddGlow(pixels, width, height, width * 0.5f, height * 0.69f, width * 0.36f, new Color(1f, 0.54f, 0.15f, 0.17f));
            DrawCircle(pixels, width, height, width * 0.73f, height * 0.78f, width * 0.105f, new Color32(244, 199, 111, 255));
            DrawTriangle(pixels, width, height, new Vector2(0, 390), new Vector2(380, 760), new Vector2(760, 390), new Color32(65, 85, 75, 255));
            DrawTriangle(pixels, width, height, new Vector2(330, 360), new Vector2(725, 790), new Vector2(1024, 400), new Color32(45, 66, 62, 255));
            DrawTriangle(pixels, width, height, new Vector2(0, 250), new Vector2(520, 560), new Vector2(1024, 230), new Color32(34, 55, 54, 255));

            DrawCabin(pixels, width, height, 250, 268, 150, 110, new Color32(112, 80, 53, 255));
            DrawCabin(pixels, width, height, 640, 250, 165, 125, new Color32(129, 91, 57, 255));
            DrawLine(pixels, width, height, new Vector2(417, 174), new Vector2(607, 174), 31, new Color32(72, 50, 37, 255));
            DrawTriangle(pixels, width, height, new Vector2(485, 175), new Vector2(512, 352), new Vector2(542, 175), new Color32(232, 101, 39, 255));
            DrawTriangle(pixels, width, height, new Vector2(493, 179), new Vector2(514, 291), new Vector2(532, 179), new Color32(255, 201, 90, 255));
            DrawCircle(pixels, width, height, 512, 170, 19, new Color32(238, 184, 103, 255));
            return ToTexture(width, height, pixels);
        }

        private static Texture2D BuildLoadingBackground(int width, int height)
        {
            var pixels = Gradient(width, height, new Color32(68, 101, 112, 255), new Color32(202, 157, 98, 255));
            AddGlow(pixels, width, height, width * 0.72f, height * 0.69f, width * 0.2f, new Color(1f, 0.7f, 0.3f, 0.22f));
            DrawCircle(pixels, width, height, width * 0.72f, height * 0.68f, 76, new Color32(248, 208, 134, 255));
            DrawHills(pixels, width, height, 0.34f, 0.12f, new Color32(106, 124, 95, 255), 1);
            DrawHills(pixels, width, height, 0.25f, 0.075f, new Color32(66, 83, 66, 255), 2);
            DrawHills(pixels, width, height, 0.15f, 0.035f, new Color32(42, 53, 43, 255), 3);

            DrawTree(pixels, width, height, 130, 150, 115, new Color32(37, 58, 48, 255));
            DrawTree(pixels, width, height, 290, 125, 82, new Color32(43, 61, 48, 255));
            DrawTree(pixels, width, height, 1640, 124, 100, new Color32(38, 57, 48, 255));
            DrawTree(pixels, width, height, 1810, 140, 125, new Color32(31, 50, 45, 255));
            DrawCabin(pixels, width, height, 690, 114, 180, 124, new Color32(105, 75, 52, 255));
            DrawCabin(pixels, width, height, 1030, 132, 228, 154, new Color32(121, 84, 55, 255));
            DrawCabin(pixels, width, height, 1325, 116, 174, 126, new Color32(103, 73, 50, 255));

            AddGlow(pixels, width, height, width * 0.5f, 0.19f * height, 130f, new Color(1f, 0.45f, 0.14f, 0.26f));
            DrawLine(pixels, width, height, new Vector2(width * 0.5f - 40f, 0.115f * height),
                new Vector2(width * 0.5f + 40f, 0.115f * height), 16f, new Color32(78, 51, 35, 255));
            DrawTriangle(pixels, width, height, new Vector2(width * 0.5f - 25f, 0.12f * height),
                new Vector2(width * 0.5f, 0.29f * height), new Vector2(width * 0.5f + 28f, 0.12f * height),
                new Color32(235, 105, 45, 255));
            DrawTriangle(pixels, width, height, new Vector2(width * 0.5f - 13f, 0.12f * height),
                new Vector2(width * 0.5f, 0.235f * height), new Vector2(width * 0.5f + 14f, 0.12f * height),
                new Color32(255, 205, 101, 255));
            return ToTexture(width, height, pixels);
        }

        private static Color[] Gradient(int width, int height, Color top, Color bottom)
        {
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (float)(height - 1));
                var row = y * width;
                for (var x = 0; x < width; x++)
                {
                    var edge = Mathf.InverseLerp(0f, width * 0.5f, Mathf.Min(x, width - x));
                    pixels[row + x] = Color.Lerp(color * 0.78f, color, edge);
                }
            }
            return pixels;
        }

        private static void AddGlow(Color[] pixels, int width, int height, float cx, float cy, float radius, Color glow)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            var maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius));
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / radius;
                    if (distance >= 1f)
                        continue;
                    var color = glow;
                    color.a *= 1f - distance;
                    var index = y * width + x;
                    pixels[index] = Color.Lerp(pixels[index], color, color.a);
                }
            }
        }

        private static void DrawCircle(Color[] pixels, int width, int height, float cx, float cy, float radius, Color color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            var maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius));
            var r2 = radius * radius;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                        pixels[y * width + x] = color;
                }
            }
        }

        private static void DrawTriangle(Color[] pixels, int width, int height, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, width - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, width - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, height - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, height - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x, y);
                    var d1 = Sign(p, a, b);
                    var d2 = Sign(p, b, c);
                    var d3 = Sign(p, c, a);
                    if (!((d1 < 0f || d2 < 0f || d3 < 0f) && (d1 > 0f || d2 > 0f || d3 > 0f)))
                        pixels[y * width + x] = color;
                }
            }
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        private static void DrawLine(Color[] pixels, int width, int height, Vector2 a, Vector2 b, float thickness, Color color)
        {
            var radius = thickness * 0.5f;
            var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius));
            var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius));
            var maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius));
            var ab = b - a;
            var lengthSquared = ab.sqrMagnitude;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var point = new Vector2(x, y);
                    var t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared) : 0f;
                    if ((point - (a + ab * t)).sqrMagnitude <= radius * radius)
                        pixels[y * width + x] = color;
                }
            }
        }

        private static void DrawHills(Color[] pixels, int width, int height, float baseHeight, float amplitude, Color color, int seed)
        {
            for (var x = 0; x < width; x++)
            {
                var n = Mathf.PerlinNoise((x + seed * 701) * 0.0023f, seed * 0.137f);
                var ridge = baseHeight + (n - 0.5f) * amplitude + Mathf.Sin(x * 0.006f + seed) * amplitude * 0.12f;
                var top = Mathf.Clamp(Mathf.RoundToInt(ridge * height), 0, height - 1);
                for (var y = 0; y <= top; y++)
                    pixels[y * width + x] = color;
            }
        }

        private static void DrawTree(Color[] pixels, int width, int height, int x, int y, int size, Color color)
        {
            DrawLine(pixels, width, height, new Vector2(x, y), new Vector2(x, y + size * 0.48f), size * 0.11f,
                new Color32(83, 59, 41, 255));
            DrawTriangle(pixels, width, height, new Vector2(x - size * 0.48f, y + size * 0.24f),
                new Vector2(x, y + size), new Vector2(x + size * 0.48f, y + size * 0.24f), color);
            DrawTriangle(pixels, width, height, new Vector2(x - size * 0.38f, y + size * 0.48f),
                new Vector2(x, y + size * 1.18f), new Vector2(x + size * 0.38f, y + size * 0.48f), color);
        }

        private static void DrawCabin(Color[] pixels, int width, int height, int x, int y, int cabinWidth, int cabinHeight, Color wall)
        {
            var left = Mathf.Clamp(x, 0, width - 1);
            var right = Mathf.Clamp(x + cabinWidth, 0, width - 1);
            var bottom = Mathf.Clamp(y, 0, height - 1);
            var top = Mathf.Clamp(y + cabinHeight, 0, height - 1);
            var roof = new Color32(66, 48, 37, 255);

            for (var py = bottom; py <= top; py++)
            {
                for (var px = left; px <= right; px++)
                    pixels[py * width + px] = wall;
            }

            DrawTriangle(pixels, width, height, new Vector2(x - 16, top - 2),
                new Vector2(x + cabinWidth * 0.5f, top + cabinHeight * 0.62f),
                new Vector2(x + cabinWidth + 16, top - 2), roof);
            var doorWidth = Mathf.Max(18, cabinWidth / 7);
            for (var py = bottom; py < bottom + cabinHeight * 0.68f; py++)
            {
                for (var px = x + cabinWidth * 0.5f - doorWidth * 0.5f; px < x + cabinWidth * 0.5f + doorWidth * 0.5f; px++)
                {
                    if (px >= 0 && px < width && py >= 0 && py < height)
                        pixels[py * width + px] = new Color32(48, 39, 31, 255);
                }
            }
        }

        private static Texture2D ToTexture(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = "Founders Generated Branding"
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
#endif
