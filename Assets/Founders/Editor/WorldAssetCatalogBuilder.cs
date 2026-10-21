#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Founders.World;
using UnityEditor;
using UnityEngine;

namespace Founders.EditorTools
{
    internal static class WorldAssetCatalogBuilder
    {
        private const string GeneratedFolder = "Assets/Founders/Generated";
        private const string CatalogPath = GeneratedFolder + "/WorldAssetCatalog.asset";
        private const string SettingsPath = GeneratedFolder + "/WorldGenerationSettings.asset";

        private static readonly string[] VegetationTokens =
        {
            "tree", "pine", "vegetation", "plant", "bush", "shrub", "flower", "grass",
            "fern", "mushroom", "vegetable", "grain", "crop", "leaf", "foliage"
        };
        private static readonly string[] RockTokens =
        {
            "rock", "stone", "cliff", "boulder", "obelisk", "canyon", "pebble", "pavement"
        };
        private static readonly string[] WaterTokens =
        {
            "water", "lake", "river", "shore", "waterfall", "pond", "stream"
        };
        private static readonly string[] BuildingTokens =
        {
            "hut", "house", "tent", "camp", "shrine", "tower", "bridge", "building", "structure",
            "gate", "wall", "shelter"
        };
        private static readonly string[] CharacterTokens =
        {
            "character", "human", "male", "female", "body", "hair", "clothing", "skin"
        };
        private static readonly string[] EffectTokens =
        {
            "particle", "fire", "fog", "butterfly", "swarm", "sunshaft", "smoke", "effect"
        };

        [MenuItem("Founders/World/Build Asset Catalog")]
        internal static void BuildCatalog()
        {
            EnsureGeneratedFolder();

            var vegetation = new List<GameObject>();
            var rocks = new List<GameObject>();
            var water = new List<GameObject>();
            var buildings = new List<GameObject>();
            var characters = new List<GameObject>();
            var effects = new List<GameObject>();
            var environment = new List<GameObject>();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                         .OrderBy(guid => AssetDatabase.GUIDToAssetPath(guid), StringComparer.OrdinalIgnoreCase))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.StartsWith(GeneratedFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                environment.Add(prefab);
                var parentFolder = Path.GetFileName(Path.GetDirectoryName(path));
                var searchable = (Path.GetFileNameWithoutExtension(path) + " " + parentFolder).ToLowerInvariant();

                if (ContainsAny(searchable, VegetationTokens))
                    vegetation.Add(prefab);
                else if (ContainsAny(searchable, RockTokens))
                    rocks.Add(prefab);
                else if (ContainsAny(searchable, WaterTokens))
                    water.Add(prefab);
                else if (ContainsAny(searchable, BuildingTokens))
                    buildings.Add(prefab);
                else if (ContainsAny(searchable, CharacterTokens))
                    characters.Add(prefab);
                else if (ContainsAny(searchable, EffectTokens))
                    effects.Add(prefab);
            }

            EnsureFallbackBuildings(buildings);

            var terrainLayers = AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(AssetDatabase.LoadAssetAtPath<TerrainLayer>)
                .Where(layer => layer != null)
                .ToArray();

            var interfaceSprites = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                .Where(sprite => sprite != null)
                .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var skyMaterials = AssetDatabase.FindAssets("t:Material", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => AssetDatabase.LoadAssetAtPath<Material>(path))
                .Where(material => material != null &&
                    ((material.shader != null && material.shader.name.IndexOf("skybox", StringComparison.OrdinalIgnoreCase) >= 0) ||
                     material.name.IndexOf("sky", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();

            var catalog = AssetDatabase.LoadAssetAtPath<WorldAssetCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorldAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.vegetationPrefabs = vegetation.Distinct().ToArray();
            catalog.rockPrefabs = rocks.Distinct().ToArray();
            catalog.waterPrefabs = water.Distinct().ToArray();
            catalog.buildingPrefabs = buildings.Distinct().ToArray();
            catalog.characterPrefabs = characters.Distinct().ToArray();
            catalog.effectPrefabs = effects.Distinct().ToArray();
            catalog.terrainLayers = terrainLayers;
            catalog.environmentPrefabs = environment.Distinct().ToArray();
            catalog.skyMaterials = skyMaterials.Distinct().ToArray();
            catalog.interfaceSprites = interfaceSprites;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Founders world catalog created at " + CatalogPath +
                ". Prefabs: " + environment.Count +
                ", vegetation: " + vegetation.Count +
                ", rocks: " + rocks.Count +
                ", water: " + water.Count +
                ", buildings: " + buildings.Count +
                ", characters: " + characters.Count +
                ", effects: " + effects.Count +
                ", terrain layers: " + terrainLayers.Length +
                ", sky materials: " + skyMaterials.Length +
                ", interface sprites: " + interfaceSprites.Length +
                ". Review the catalog before building a scene.");
            Selection.activeObject = catalog;
        }

        [MenuItem("Founders/World/Create Default Generation Settings")]
        internal static void CreateSettings()
        {
            EnsureGeneratedFolder();

            var settings = AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<WorldGenerationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        private static void EnsureFallbackBuildings(List<GameObject> buildings)
        {
            var generatedBuildings = "Assets/Founders/Generated/Blockout";
            Directory.CreateDirectory(generatedBuildings);

            EnsureFallback(buildings, generatedBuildings + "/Blockout_Campfire.prefab", "Blockout_Campfire", "campfire",
                new Color(0.92f, 0.42f, 0.12f, 1f), root =>
                {
                    AddPrimitive(root, PrimitiveType.Cylinder, "Log A", new Vector3(-0.45f, 0.13f, 0f),
                        new Vector3(1.5f, 0.14f, 0.14f), new Vector3(0f, 0f, 90f));
                    AddPrimitive(root, PrimitiveType.Cylinder, "Log B", new Vector3(0.45f, 0.13f, 0f),
                        new Vector3(1.5f, 0.14f, 0.14f), new Vector3(0f, 0f, -90f));
                    AddPrimitive(root, PrimitiveType.Sphere, "Flame", new Vector3(0f, 0.85f, 0f),
                        new Vector3(0.75f, 1.25f, 0.75f), Vector3.zero);
                });
            EnsureFallback(buildings, generatedBuildings + "/Blockout_Tent.prefab", "Blockout_Tent", "tent",
                new Color(0.66f, 0.49f, 0.3f, 1f), root =>
                {
                    AddPrimitive(root, PrimitiveType.Cube, "Canvas shelter", new Vector3(0f, 0.8f, 0f),
                        new Vector3(3.2f, 1.6f, 2.8f), Vector3.zero);
                    AddPrimitive(root, PrimitiveType.Cylinder, "Center pole", new Vector3(0f, 1.8f, 0f),
                        new Vector3(0.12f, 1.2f, 0.12f), Vector3.zero);
                });
            EnsureFallback(buildings, generatedBuildings + "/Blockout_Hut.prefab", "Blockout_Hut", "hut",
                new Color(0.46f, 0.31f, 0.19f, 1f), root =>
                {
                    AddPrimitive(root, PrimitiveType.Cube, "Hut walls", new Vector3(0f, 1.1f, 0f),
                        new Vector3(3.6f, 2.2f, 3.2f), Vector3.zero);
                    AddPrimitive(root, PrimitiveType.Cube, "Roof side A", new Vector3(-0.8f, 2.35f, 0f),
                        new Vector3(2.2f, 0.2f, 3.7f), new Vector3(0f, 0f, -34f));
                    AddPrimitive(root, PrimitiveType.Cube, "Roof side B", new Vector3(0.8f, 2.35f, 0f),
                        new Vector3(2.2f, 0.2f, 3.7f), new Vector3(0f, 0f, 34f));
                });
            EnsureFallback(buildings, generatedBuildings + "/Blockout_Farm.prefab", "Blockout_Farm", "farm",
                new Color(0.38f, 0.47f, 0.25f, 1f), root =>
                {
                    AddPrimitive(root, PrimitiveType.Cube, "Farm plot", new Vector3(0f, 0.08f, 0f),
                        new Vector3(5f, 0.16f, 4f), Vector3.zero);
                    for (var row = -1; row <= 1; row++)
                    {
                        for (var column = -2; column <= 2; column++)
                            AddPrimitive(root, PrimitiveType.Sphere, "Crop", new Vector3(column * 0.72f, 0.48f, row * 0.9f),
                                new Vector3(0.34f, 0.78f, 0.34f), Vector3.zero);
                    }
                });
        }

        private static void EnsureFallback(
            List<GameObject> buildings,
            string path,
            string objectName,
            string token,
            Color tint,
            Action<GameObject> build)
        {
            var found = buildings.Exists(prefab =>
                prefab != null && prefab.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            if (found)
                return;

            var materialPath = "Assets/Founders/Generated/Blockout/" + objectName + "_Material.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                material = new Material(shader) { color = tint };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var root = new GameObject(objectName);
            build(root);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab != null)
                buildings.Add(prefab);
        }

        private static void AddPrimitive(
            GameObject parent,
            PrimitiveType primitiveType,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Vector3 eulerAngles)
        {
            var child = GameObject.CreatePrimitive(primitiveType);
            child.name = objectName;
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = position;
            child.transform.localScale = scale;
            child.transform.localEulerAngles = eulerAngles;
        }

        private static bool ContainsAny(string value, IEnumerable<string> tokens)
        {
            return tokens.Any(value.Contains);
        }

        private static void EnsureGeneratedFolder()
        {
            Directory.CreateDirectory(GeneratedFolder);
            AssetDatabase.Refresh();
        }
    }
}
#endif
