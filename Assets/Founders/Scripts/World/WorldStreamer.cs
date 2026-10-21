using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Founders.World
{
    /// <summary>
    /// Streams a deterministic, square world around the camera/player. The configured
    /// world is 64 x 64 chunks at 256m per chunk (16.384km across); only nearby chunks
    /// and their natural props are instantiated.
    /// </summary>
    public sealed class WorldStreamer : MonoBehaviour
    {
        public static event Action FirstChunkGenerated;
        public static bool HasGeneratedAnyChunk { get; private set; }
        [SerializeField] private WorldGenerationSettings settings;
        [SerializeField] private WorldAssetCatalog assetCatalog;
        [SerializeField] private Transform focus;
        [SerializeField, Min(0.1f)] private float refreshSeconds = 0.5f;
        [SerializeField, Range(1, 5)] private int maxChunksBuiltPerFrame = 1;

        private readonly Dictionary<Vector2Int, ChunkInstance> loaded = new Dictionary<Vector2Int, ChunkInstance>();
        private readonly HashSet<Vector2Int> wanted = new HashSet<Vector2Int>();
        private Coroutine streamRoutine;
        private GameObject waterSurface;
        private Material runtimeWaterMaterial;
        private Mesh waterMesh;

        private sealed class ChunkInstance
        {
            public Terrain terrain;
            public TerrainData data;
            public Transform props;
        }

        private void OnEnable()
        {
            if (streamRoutine == null)
                streamRoutine = StartCoroutine(StreamLoop());
        }

        private void OnDisable()
        {
            if (streamRoutine != null)
                StopCoroutine(streamRoutine);
            streamRoutine = null;
            ClearWorld();
        }

        private IEnumerator StreamLoop()
        {
            while (true)
            {
                if (focus == null && Camera.main != null)
                    focus = Camera.main.transform;

                if (focus != null && settings != null)
                {
                    EnsureWaterSurface();
                    RefreshWantedChunks();
                    UnloadUnwantedChunks();

                    var built = 0;
                    foreach (var coordinate in wanted)
                    {
                        if (loaded.ContainsKey(coordinate))
                            continue;

                        BuildChunk(coordinate);
                        built++;
                        if (built >= maxChunksBuiltPerFrame)
                            break;
                    }

                    UpdateNeighbors();
                }

                yield return new WaitForSeconds(refreshSeconds);
            }
        }

        private void RefreshWantedChunks()
        {
            wanted.Clear();
            var center = WorldToChunk(focus.position);
            var radius = Mathf.Max(1, settings.visibleChunkRadius);

            for (var z = center.y - radius; z <= center.y + radius; z++)
            {
                for (var x = center.x - radius; x <= center.x + radius; x++)
                {
                    if (settings.ContainsChunk(x, z))
                        wanted.Add(new Vector2Int(x, z));
                }
            }
        }

        private Vector2Int WorldToChunk(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / settings.chunkSizeMeters),
                Mathf.FloorToInt(position.z / settings.chunkSizeMeters));
        }

        private void BuildChunk(Vector2Int coordinate)
        {
            var data = new TerrainData
            {
                heightmapResolution = settings.heightmapResolution,
                size = new Vector3(settings.chunkSizeMeters, settings.terrainHeightMeters, settings.chunkSizeMeters),
                alphamapResolution = Mathf.Clamp(settings.heightmapResolution - 1, 32, 512)
            };

            BuildHeights(data, coordinate);
            ApplyTerrainLayers(data, coordinate);

            var terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "World Chunk " + coordinate.x + "," + coordinate.y;
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.position = new Vector3(
                coordinate.x * settings.chunkSizeMeters,
                0f,
                coordinate.y * settings.chunkSizeMeters);

            var terrain = terrainObject.GetComponent<Terrain>();
            terrain.heightmapPixelError = 20f;
            terrain.basemapDistance = 1200f;
            terrain.drawInstanced = true;

            var propRoot = new GameObject("Natural Props");
            propRoot.transform.SetParent(terrainObject.transform, false);
            ScatterProps(coordinate, data, propRoot.transform);

            loaded.Add(coordinate, new ChunkInstance
            {
                terrain = terrain,
                data = data,
                props = propRoot.transform
            });

            if (!HasGeneratedAnyChunk)
            {
                HasGeneratedAnyChunk = true;
                FirstChunkGenerated?.Invoke();
            }
        }

        private void BuildHeights(TerrainData data, Vector2Int coordinate)
        {
            var resolution = settings.heightmapResolution;
            var heights = new float[resolution, resolution];
            var worldOriginX = coordinate.x * settings.chunkSizeMeters;
            var worldOriginZ = coordinate.y * settings.chunkSizeMeters;
            var denominator = resolution - 1f;

            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var worldX = worldOriginX + x / denominator * settings.chunkSizeMeters;
                    var worldZ = worldOriginZ + z / denominator * settings.chunkSizeMeters;
                    heights[z, x] = SampleHeight(worldX, worldZ);
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private float SampleHeight(float worldX, float worldZ)
        {
            var seedOffset = settings.worldSeed * 0.0137f;
            var broad = Mathf.PerlinNoise(
                (worldX + seedOffset) / settings.broadNoiseScaleMeters,
                (worldZ - seedOffset) / settings.broadNoiseScaleMeters);
            var detail = Mathf.PerlinNoise(
                (worldX - seedOffset * 2f) / settings.detailNoiseScaleMeters,
                (worldZ + seedOffset * 2f) / settings.detailNoiseScaleMeters);

            var halfWorld = settings.WorldSizeMeters * 0.5f;
            var edgeDistance = Mathf.Max(Mathf.Abs(worldX), Mathf.Abs(worldZ)) / halfWorld;
            var fadeStart = 1f - settings.shorelineFalloff;
            var edgeBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, fadeStart, edgeDistance));
            var land = settings.shorelineHeight + (broad - 0.35f) * 0.74f + (detail - 0.5f) * 0.1f;
            return Mathf.Lerp(settings.shorelineHeight, Mathf.Clamp01(land), edgeBlend);
        }

        private void ApplyTerrainLayers(TerrainData data, Vector2Int coordinate)
        {
            if (assetCatalog == null || assetCatalog.terrainLayers == null || assetCatalog.terrainLayers.Length == 0)
                return;

            var sourceLayers = assetCatalog.terrainLayers;
            var layerCount = Mathf.Min(4, sourceLayers.Length);
            var layers = new TerrainLayer[layerCount];
            Array.Copy(sourceLayers, layers, layerCount);
            data.terrainLayers = layers;

            var resolution = data.alphamapResolution;
            var alpha = new float[resolution, resolution, layerCount];
            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var u = x / (resolution - 1f);
                    var v = z / (resolution - 1f);
                    var worldX = data.size.x * u;
                    var worldZ = data.size.z * v;
                    var globalX = coordinate.x * settings.chunkSizeMeters + worldX;
                    var globalZ = coordinate.y * settings.chunkSizeMeters + worldZ;
                    var height = SampleHeight(globalX, globalZ);

                    var moisture = Mathf.PerlinNoise(
                        (globalX + settings.worldSeed) / 420f,
                        (globalZ - settings.worldSeed) / 420f);
                    var rock = Mathf.Clamp01((height - 0.48f) * 3f);
                    var wet = Mathf.Clamp01((moisture - 0.34f) * 2.6f) * (1f - rock);
                    var dry = Mathf.Max(0f, 1f - rock - wet);

                    if (layerCount == 1)
                    {
                        alpha[z, x, 0] = 1f;
                    }
                    else if (layerCount == 2)
                    {
                        alpha[z, x, 0] = dry;
                        alpha[z, x, 1] = 1f - dry;
                    }
                    else if (layerCount == 3)
                    {
                        alpha[z, x, 0] = dry;
                        alpha[z, x, 1] = wet;
                        alpha[z, x, 2] = rock;
                        NormalizeWeights(alpha, z, x, layerCount);
                    }
                    else
                    {
                        alpha[z, x, 0] = dry * (1f - moisture * 0.35f);
                        alpha[z, x, 1] = wet;
                        alpha[z, x, 2] = rock;
                        alpha[z, x, 3] = dry * moisture * 0.35f;
                        NormalizeWeights(alpha, z, x, layerCount);
                    }
                }
            }

            data.SetAlphamaps(0, 0, alpha);
        }

        private static void NormalizeWeights(float[,,] alpha, int z, int x, int count)
        {
            var total = 0f;
            for (var i = 0; i < count; i++)
                total += alpha[z, x, i];

            if (total <= 0f)
            {
                alpha[z, x, 0] = 1f;
                return;
            }

            for (var i = 0; i < count; i++)
                alpha[z, x, i] /= total;
        }

        private void ScatterProps(Vector2Int coordinate, TerrainData data, Transform root)
        {
            if (assetCatalog == null)
                return;

            var random = new System.Random(ChunkSeed(coordinate));
            ScatterCategory(random, coordinate, data, root, assetCatalog.vegetationPrefabs, settings.vegetationInstancesPerChunk, NaturalPropKind.Vegetation);
            ScatterCategory(random, coordinate, data, root, assetCatalog.rockPrefabs, settings.rockInstancesPerChunk, NaturalPropKind.Rock);
        }

        private void ScatterCategory(
            System.Random random,
            Vector2Int coordinate,
            TerrainData data,
            Transform root,
            GameObject[] prefabs,
            int count,
            NaturalPropKind kind)
        {
            if (prefabs == null || prefabs.Length == 0 || count <= 0)
                return;

            var originX = coordinate.x * settings.chunkSizeMeters;
            var originZ = coordinate.y * settings.chunkSizeMeters;

            for (var i = 0; i < count; i++)
            {
                var localX = (float)random.NextDouble() * settings.chunkSizeMeters;
                var localZ = (float)random.NextDouble() * settings.chunkSizeMeters;
                var worldX = originX + localX;
                var worldZ = originZ + localZ;
                var normalizedHeight = SampleHeight(worldX, worldZ);

                if (normalizedHeight <= settings.shorelineHeight + 0.035f)
                    continue;

                var prefab = prefabs[random.Next(prefabs.Length)];
                var yaw = (float)random.NextDouble() * 360f;
                var scale = Mathf.Lerp(
                    settings.minimumPropScale,
                    settings.maximumPropScale,
                    (float)random.NextDouble());
                var stableId = coordinate.x + ":" + coordinate.y + ":" + (int)kind + ":" + i;
                var save = WorldSaveSystem.Instance;
                if (save != null && save.WasNaturalPropCleared(stableId))
                    continue;
                if (prefab == null)
                    continue;

                var worldY = normalizedHeight * settings.terrainHeightMeters;
                var instance = Instantiate(
                    prefab,
                    new Vector3(worldX, worldY, worldZ),
                    Quaternion.Euler(0f, yaw, 0f),
                    root);

                instance.transform.localScale *= scale;

                var marker = instance.AddComponent<NaturalPropMarker>();
                marker.kind = kind;
                marker.stableId = stableId;
                marker.EnsureBoundsCollider();
            }
        }

        private int ChunkSeed(Vector2Int coordinate)
        {
            unchecked
            {
                return settings.worldSeed * 397 ^ coordinate.x * 7919 ^ coordinate.y * 104729;
            }
        }

        private void UnloadUnwantedChunks()
        {
            var remove = new List<Vector2Int>();
            foreach (var coordinate in loaded.Keys)
            {
                if (!wanted.Contains(coordinate))
                    remove.Add(coordinate);
            }

            foreach (var coordinate in remove)
            {
                var chunk = loaded[coordinate];
                if (chunk.terrain != null)
                    Destroy(chunk.terrain.gameObject);
                if (chunk.data != null)
                    Destroy(chunk.data);
                loaded.Remove(coordinate);
            }
        }

        private void UpdateNeighbors()
        {
            foreach (var pair in loaded)
            {
                var coordinate = pair.Key;
                loaded.TryGetValue(coordinate + Vector2Int.left, out var left);
                loaded.TryGetValue(coordinate + Vector2Int.right, out var right);
                loaded.TryGetValue(coordinate + Vector2Int.down, out var bottom);
                loaded.TryGetValue(coordinate + Vector2Int.up, out var top);

                pair.Value.terrain.SetNeighbors(
                    left != null ? left.terrain : null,
                    top != null ? top.terrain : null,
                    right != null ? right.terrain : null,
                    bottom != null ? bottom.terrain : null);
            }
        }

        private void EnsureWaterSurface()
        {
            if (waterSurface != null)
                return;

            var half = settings.WorldSizeMeters * 0.5f;
            waterMesh = new Mesh { name = "Founders World Water" };
            waterMesh.vertices = new[]
            {
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(half, 0f, half),
                new Vector3(-half, 0f, half)
            };
            waterMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            waterMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();

            waterSurface = new GameObject("World Water Surface");
            waterSurface.transform.SetParent(transform, false);
            waterSurface.transform.position = new Vector3(
                0f,
                settings.shorelineHeight * settings.terrainHeightMeters,
                0f);

            var filter = waterSurface.AddComponent<MeshFilter>();
            filter.sharedMesh = waterMesh;
            var renderer = waterSurface.AddComponent<MeshRenderer>();
            runtimeWaterMaterial = FindWaterMaterial();
            if (runtimeWaterMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                if (shader != null)
                {
                    runtimeWaterMaterial = new Material(shader);
                    runtimeWaterMaterial.color = new Color(0.24f, 0.58f, 0.62f, 1f);
                }
            }

            renderer.sharedMaterial = runtimeWaterMaterial;
            if (runtimeWaterMaterial != null && runtimeWaterMaterial.mainTexture != null)
            {
                var tileCount = settings.WorldSizeMeters / 64f;
                runtimeWaterMaterial.mainTextureScale = new Vector2(tileCount, tileCount);
            }
        }

        private Material FindWaterMaterial()
        {
            if (assetCatalog == null || assetCatalog.waterPrefabs == null)
                return null;

            foreach (var prefab in assetCatalog.waterPrefabs)
            {
                if (prefab == null || prefab.name.IndexOf("waterfall", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (var prefabRenderer in renderers)
                {
                    if (prefabRenderer.sharedMaterial != null)
                        return new Material(prefabRenderer.sharedMaterial);
                }
            }

            return null;
        }

        private void ClearWorld()
        {
            if (waterSurface != null)
                Destroy(waterSurface);
            waterSurface = null;
            if (runtimeWaterMaterial != null)
                Destroy(runtimeWaterMaterial);
            runtimeWaterMaterial = null;
            if (waterMesh != null)
                Destroy(waterMesh);
            waterMesh = null;

            foreach (var chunk in loaded.Values)
            {
                if (chunk.terrain != null)
                    Destroy(chunk.terrain.gameObject);
                if (chunk.data != null)
                    Destroy(chunk.data);
            }
            loaded.Clear();
            wanted.Clear();
            HasGeneratedAnyChunk = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (settings == null)
                return;

            Gizmos.color = new Color(0.3f, 0.8f, 0.35f, 0.8f);
            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(settings.WorldSizeMeters, 5f, settings.WorldSizeMeters));
        }
#endif
    }
}
