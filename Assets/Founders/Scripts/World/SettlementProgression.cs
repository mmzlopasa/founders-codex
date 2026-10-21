using System;
using System.Collections.Generic;
using UnityEngine;

namespace Founders.World
{
    /// <summary>
    /// Campfire-led progression, construction orders, and consumable construction
    /// speedups. Building assets are looked up from the imported project catalog.
    /// </summary>
    public sealed class SettlementProgression : MonoBehaviour
    {
        private const string SaveKey = "Founders.Progression.v1";
        public static SettlementProgression Instance { get; private set; }

        [SerializeField] private WorldAssetCatalog assetCatalog;
        [SerializeField] private Founders.Seasons.SeasonDirector seasonDirector;
        [SerializeField] private bool showHud = true;

        [Serializable]
        private sealed class BuildOrder
        {
            public int prefabIndex;
            public string prefabName;
            public Vector3 position;
            public Vector3 eulerAngles;
            public float secondsRemaining;
        }

        [Serializable]
        private sealed class ProgressData
        {
            public bool campfirePlaced;
            public int campfireLevel = 1;
            public List<BuildOrder> orders = new List<BuildOrder>();
        }

        private ProgressData data = new ProgressData();
        private string message = string.Empty;
        private float saveTimer;

        public bool NeedsCampfire => !data.campfirePlaced;
        public int CampfireLevel => data.campfireLevel;
        public int QueuedBuildings => data.orders.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (data.orders.Count == 0)
                return;

            data.orders[0].secondsRemaining -= Time.deltaTime;
            if (data.orders[0].secondsRemaining <= 0f)
            {
                CompleteFirstOrder();
            }
            else
            {
                saveTimer += Time.deltaTime;
                if (saveTimer >= 5f)
                {
                    saveTimer = 0f;
                    Save();
                }
            }
        }

        public void RegisterCampfire()
        {
            data.campfirePlaced = true;
            data.campfireLevel = Mathf.Max(1, data.campfireLevel);
            message = "Campfire founded. Your settlement begins here.";
            Save();
        }

        public bool IsUnlocked(string buildingName)
        {
            if (NeedsCampfire)
                return false;
            return GetRequiredLevel(buildingName) <= data.campfireLevel;
        }

        public int GetRequiredLevel(string buildingName)
        {
            var name = (buildingName ?? string.Empty).ToLowerInvariant();
            if (ContainsAny(name, "tower", "bridge", "gate", "great hall"))
                return 4;
            if (ContainsAny(name, "workshop", "forge", "shrine", "barn", "stable", "longhouse"))
                return 3;
            if (ContainsAny(name, "hut", "house", "home", "farm", "field", "store", "storage"))
                return 2;
            return 1;
        }

        public bool TryStartConstruction(int prefabIndex, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return false;
            if (NeedsCampfire)
            {
                message = "Place your campfire first.";
                return false;
            }
            if (!IsUnlocked(prefab.name))
            {
                message = prefab.name + " unlocks at Campfire level " + GetRequiredLevel(prefab.name) + ".";
                return false;
            }

            var capacity = data.campfireLevel >= 3 ? 2 : 1;
            if (data.orders.Count >= capacity)
            {
                message = "All construction slots are busy.";
                return false;
            }

            var save = WorldSaveSystem.Instance;
            var cost = GetBuildCost(prefab.name);
            if (save == null || !save.TrySpendResources(cost.x, cost.y, cost.z))
            {
                message = "Gather more food, wood, and stone to build " + prefab.name + ".";
                return false;
            }

            data.orders.Add(new BuildOrder
            {
                prefabIndex = prefabIndex,
                prefabName = prefab.name,
                position = position,
                eulerAngles = rotation.eulerAngles,
                secondsRemaining = GetBuildTime(prefab.name)
            });
            message = "Construction started: " + prefab.name + ".";
            Save();
            return true;
        }

        public bool TryUpgradeCampfire()
        {
            if (NeedsCampfire)
            {
                message = "Place your campfire before upgrading it.";
                return false;
            }
            if (data.campfireLevel >= 10)
            {
                message = "Campfire is at maximum level.";
                return false;
            }

            var level = data.campfireLevel;
            var food = level * 20;
            var wood = level * 30;
            var stone = level * 15;
            var save = WorldSaveSystem.Instance;
            if (save == null || !save.TrySpendResources(food, wood, stone))
            {
                message = "Upgrade needs " + food + " food, " + wood + " wood, and " + stone + " stone.";
                return false;
            }

            data.campfireLevel++;
            message = "Campfire upgraded to level " + data.campfireLevel + ". New buildings are unlocked.";
            Save();
            return true;
        }

        public bool TryUseConstructionSpeedup()
        {
            if (data.orders.Count == 0)
            {
                message = "There is no active construction.";
                return false;
            }

            var save = WorldSaveSystem.Instance;
            if (save == null || !save.TrySpendConstructionSpeedup())
            {
                message = "No construction speedups remain.";
                return false;
            }

            data.orders[0].secondsRemaining -= 60f;
            message = "Construction speedup used.";
            if (data.orders[0].secondsRemaining <= 0f)
                CompleteFirstOrder();
            else
                Save();
            return true;
        }

        public void SetFarmingMessage(string text)
        {
            message = text;
        }

        private void CompleteFirstOrder()
        {
            if (data.orders.Count == 0)
                return;

            var order = data.orders[0];
            var prefab = FindPrefab(order);
            if (prefab == null)
            {
                message = "Missing building prefab: " + order.prefabName;
                Debug.LogError(message);
                return;
            }

            Instantiate(prefab, order.position, Quaternion.Euler(order.eulerAngles));
            var save = WorldSaveSystem.Instance;
            if (save != null)
                save.RegisterBuilding(order.prefabIndex, order.prefabName, order.position, Quaternion.Euler(order.eulerAngles));
            data.orders.RemoveAt(0);
            if (seasonDirector != null)
                seasonDirector.ReportProgress(Founders.Seasons.SeasonObjective.PlaceBuildings);
            message = order.prefabName + " is complete.";
            Save();
        }

        private GameObject FindPrefab(BuildOrder order)
        {
            if (assetCatalog == null || assetCatalog.buildingPrefabs == null)
                return null;

            foreach (var prefab in assetCatalog.buildingPrefabs)
            {
                if (prefab != null && prefab.name == order.prefabName)
                    return prefab;
            }

            if (order.prefabIndex >= 0 && order.prefabIndex < assetCatalog.buildingPrefabs.Length)
                return assetCatalog.buildingPrefabs[order.prefabIndex];
            return null;
        }

        private static Vector3Int GetBuildCost(string buildingName)
        {
            var name = (buildingName ?? string.Empty).ToLowerInvariant();
            if (ContainsAny(name, "farm", "field", "garden"))
                return new Vector3Int(5, 8, 0);
            if (ContainsAny(name, "house", "hut", "longhouse", "shrine"))
                return new Vector3Int(8, 18, 8);
            return new Vector3Int(3, 10, 4);
        }

        private static float GetBuildTime(string buildingName)
        {
            var name = (buildingName ?? string.Empty).ToLowerInvariant();
            if (ContainsAny(name, "house", "longhouse", "tower", "bridge"))
                return 180f;
            if (ContainsAny(name, "farm", "field", "hut", "shrine"))
                return 90f;
            return 45f;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            foreach (var token in tokens)
            {
                if (value.Contains(token))
                    return true;
            }
            return false;
        }

        private void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    data = JsonUtility.FromJson<ProgressData>(json) ?? new ProgressData();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Settlement progression save could not be read. " + exception.Message);
                    data = new ProgressData();
                }
            }

            if (data.orders == null)
                data.orders = new List<BuildOrder>();
            data.campfireLevel = Mathf.Clamp(data.campfireLevel, 1, 10);
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            if (!showHud)
                return;

            GUILayout.BeginArea(new Rect(Screen.width - 350f, 190f, 334f, 220f), GUI.skin.box);
            GUILayout.Label(NeedsCampfire ? "FOUND YOUR SETTLEMENT" : "CAMPFIRE · LEVEL " + data.campfireLevel);
            GUILayout.Label("Construction slots " + data.orders.Count + (data.campfireLevel >= 3 ? " / 2" : " / 1"));

            if (data.orders.Count > 0)
            {
                GUILayout.Label(data.orders[0].prefabName + " · " + Mathf.CeilToInt(data.orders[0].secondsRemaining) + "s");
                var save = WorldSaveSystem.Instance;
                if (save != null && GUILayout.Button("Speed up · " + save.ConstructionSpeedups + " left"))
                    TryUseConstructionSpeedup();
            }

            var buttonLabel = "Upgrade Campfire";
            if (GUILayout.Button(buttonLabel))
                TryUpgradeCampfire();
            if (!string.IsNullOrEmpty(message))
                GUILayout.Label(message);
            GUILayout.EndArea();
        }
    }
}
