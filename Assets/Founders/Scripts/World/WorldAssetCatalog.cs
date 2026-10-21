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
        [Tooltip("Discovered water environment prefabs.")]
        public GameObject[] waterPrefabs;
        [Tooltip("Prehistoric buildings and structures.")]
        public GameObject[] buildingPrefabs;
        [Tooltip("Character and character-part prefabs.")]
        public GameObject[] characterPrefabs;
        [Tooltip("Particle and environmental effect prefabs.")]
        public GameObject[] effectPrefabs;
        [Tooltip("TerrainLayers discovered in the imported landscape packs.")]
        public TerrainLayer[] terrainLayers;
        [Tooltip("All discovered environment prefabs, including unclassified items.")]
        public GameObject[] environmentPrefabs;
        [Tooltip("Sprite assets available for the Adventure UI Kit and game interface.")]
        public Sprite[] interfaceSprites;
        [Tooltip("Skybox and environment materials found in the imported packs.")]
        public Material[] skyMaterials;
    }
}
