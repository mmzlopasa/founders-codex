using UnityEngine;

namespace Founders.World
{
    [CreateAssetMenu(menuName = "Founders/World Generation Settings", fileName = "WorldGenerationSettings")]
    public sealed class WorldGenerationSettings : ScriptableObject
    {
        [Header("World scale")]
        [Min(1)] public int chunksPerSide = 64;
        [Min(32f)] public float chunkSizeMeters = 256f;
        [Range(33, 513)] public int heightmapResolution = 129;
        [Min(1)] public int visibleChunkRadius = 1;

        [Header("Terrain")]
        public int worldSeed = 381947;
        [Min(1f)] public float terrainHeightMeters = 72f;
        [Min(16f)] public float broadNoiseScaleMeters = 1100f;
        [Min(8f)] public float detailNoiseScaleMeters = 260f;
        [Range(0f, 1f)] public float shorelineHeight = 0.18f;
        [Range(0.05f, 1f)] public float shorelineFalloff = 0.28f;

        [Header("Natural props per loaded chunk")]
        [Min(0)] public int vegetationInstancesPerChunk = 180;
        [Min(0)] public int rockInstancesPerChunk = 24;
        [Range(0.5f, 1.5f)] public float minimumPropScale = 0.85f;
        [Range(0.5f, 2f)] public float maximumPropScale = 1.15f;

        public float WorldSizeMeters => chunksPerSide * chunkSizeMeters;
        public float WorldOrigin => -WorldSizeMeters * 0.5f;

        public bool ContainsChunk(int x, int z)
        {
            var half = chunksPerSide / 2;
            return x >= -half && z >= -half && x < half && z < half;
        }
    }
}
