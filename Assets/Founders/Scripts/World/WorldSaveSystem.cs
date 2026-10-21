using System;
using System.Collections.Generic;
using UnityEngine;

namespace Founders.World
{
    /// <summary>
    /// Stores player construction and cleared generated props on this device.
    /// The deterministic world can then be streamed out and rebuilt without
    /// restoring trees or losing structures.
    /// </summary>
    public sealed class WorldSaveSystem : MonoBehaviour
    {
        private const string DefaultSaveKey = "founders.longrise.world.v1";
        public static WorldSaveSystem Instance { get; private set; }

        [Serializable]
        public sealed class PlacedBuilding
        {
            public int prefabIndex;
            public string prefabName;
            public Vector3 position;
            public Vector3 eulerAngles;
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
            public List<string> clearedNaturalProps = new List<string>();
        }

        private SaveData data = new SaveData();
        private HashSet<string> clearedLookup = new HashSet<string>();

        public IReadOnlyList<PlacedBuilding> Buildings => data.buildings;

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

        public bool WasNaturalPropCleared(string stableId)
        {
            return !string.IsNullOrEmpty(stableId) && clearedLookup.Contains(stableId);
        }

        public void RegisterClearedNaturalProp(string stableId)
        {
            if (string.IsNullOrEmpty(stableId) || !clearedLookup.Add(stableId))
                return;

            data.clearedNaturalProps.Add(stableId);
            Save();
        }

        public void RegisterBuilding(int prefabIndex, string prefabName, Vector3 position, Quaternion rotation)
        {
            data.buildings.Add(new PlacedBuilding
            {
                prefabIndex = prefabIndex,
                prefabName = prefabName,
                position = position,
                eulerAngles = rotation.eulerAngles
            });
            Save();
        }

        private void Load()
        {
            var json = PlayerPrefs.GetString(DefaultSaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Founders save could not be read; starting a new settlement. " + exception.Message);
                    data = new SaveData();
                }
            }

            if (data.buildings == null)
                data.buildings = new List<PlacedBuilding>();
            if (data.clearedNaturalProps == null)
                data.clearedNaturalProps = new List<string>();
            clearedLookup = new HashSet<string>(data.clearedNaturalProps);
        }

        private void Save()
        {
            PlayerPrefs.SetString(DefaultSaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
