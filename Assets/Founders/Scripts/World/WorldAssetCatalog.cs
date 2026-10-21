using UnityEngine;

namespace Founders.World
{
    [CreateAssetMenu(menuName = "Founders/World Asset Catalog", fileName = "WorldAssetCatalog")]
    public sealed class WorldAssetCatalog : ScriptableObject
    {
        [Tooltip("All discovered vegetation prefabs from the imported nature packs.")]
        public GameObject[] vegetationPrefabs;
        [Tooltip("All discovered stone and rock prefabs.")]
        public GameObject[] rockPrefabs;
        [Tooltip("Optional TerrainLayers discovered in the imported landscape packs.")]
        public TerrainLayer[] terrainLayers;
        [Tooltip("Remaining environment prefabs, retained for future biomes and landmarks.")]
        public GameObject[] environmentPrefabs;
    }
}
