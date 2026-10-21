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
            "gate", "wall", "shelter", "village"
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

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.StartsWith(GeneratedFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                environment.Add(prefab);
                var searchable = (Path.GetFileNameWithoutExtension(path) + " " + path).ToLowerInvariant();

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

            var terrainLayers = AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(AssetDatabase.LoadAssetAtPath<TerrainLayer>)
                .Where(layer => layer != null)
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
