#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Founders.EditorTools
{
    [Serializable]
    internal sealed class AssetAuditReport
    {
        public string generatedUtc;
        public string unityVersion;
        public List<AssetAuditEntry> entries = new List<AssetAuditEntry>();
    }

    [Serializable]
    internal sealed class AssetAuditEntry
    {
        public string kind;
        public string category;
        public string name;
        public string path;
    }

    /// <summary>
    /// Creates a reproducible inventory of imported pack contents before the world
    /// generator binds to specific prefabs or materials.
    /// </summary>
    internal static class AssetAudit
    {
        private const string OutputFolder = "Assets/Founders/Generated";
        private const string OutputFile = OutputFolder + "/asset-inventory.json";

        private static readonly string[][] CategoryTokens =
        {
            new[] { "tree", "pine", "forest", "bush", "fern", "plant", "grass", "flower", "mushroom", "vegetation" },
            new[] { "rock", "stone", "cliff", "canyon", "boulder", "pebble" },
            new[] { "water", "lake", "river", "shore", "waterfall" },
            new[] { "terrain", "landscape", "ground", "mountain", "hill" },
            new[] { "hut", "house", "tent", "camp", "shrine", "tower", "bridge", "building", "structure" },
            new[] { "character", "human", "male", "female", "body", "hair", "clothing", "skin" },
            new[] { "animation", "idle", "walk", "work", "pray", "worship", "sit" },
            new[] { "prop", "tool", "pottery", "weapon", "bone", "fossil", "flag", "fence", "boat", "raft" },
            new[] { "particle", "fire", "fog", "butterfly", "swarm", "sunshaft" },
            new[] { "skybox", "sky" }
        };

        private static readonly string[] CategoryNames =
        {
            "vegetation", "rocks", "water", "terrain", "buildings",
            "characters", "animations", "props", "effects", "sky"
        };

        [MenuItem("Founders/Assets/Generate Asset Inventory")]
        private static void Generate()
        {
            var report = new AssetAuditReport
            {
                generatedUtc = DateTime.UtcNow.ToString("o"),
                unityVersion = Application.unityVersion
            };

            AddAssets(report, "Prefab", "t:Prefab");
            AddAssets(report, "AnimationClip", "t:AnimationClip");
            AddAssets(report, "Material", "t:Material");
            AddAssets(report, "Texture", "t:Texture2D");
            AddAssets(report, "TerrainLayer", "t:TerrainLayer");

            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText(OutputFile, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();

            var counts = report.entries
                .GroupBy(entry => entry.category)
                .OrderBy(group => group.Key)
                .Select(group => group.Key + ": " + group.Count());
            Debug.Log("Founders asset inventory saved to " + OutputFile + "\n" + string.Join("\n", counts));
        }

        private static void AddAssets(AssetAuditReport report, string kind, string filter)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.StartsWith(OutputFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = Path.GetFileNameWithoutExtension(path);
                report.entries.Add(new AssetAuditEntry
                {
                    kind = kind,
                    category = Categorize(name),
                    name = name,
                    path = path
                });
            }
        }

        private static string Categorize(string value)
        {
            var name = value.ToLowerInvariant();
            for (var i = 0; i < CategoryTokens.Length; i++)
            {
                if (CategoryTokens[i].Any(name.Contains))
                    return CategoryNames[i];
            }

            return "uncategorized";
        }
    }
}
#endif
