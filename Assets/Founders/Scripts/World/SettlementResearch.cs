using System;
using UnityEngine;

namespace Founders.World
{
    /// <summary>
    /// Small persistent prehistoric research tree. Research consumes settlement
    /// resources and improves worker yields or construction speed.
    /// </summary>
    public sealed class SettlementResearch : MonoBehaviour
    {
        private const string SaveKey = "Founders.Research.v1";
        public static SettlementResearch Instance { get; private set; }

        [Serializable]
        private sealed class ResearchData
        {
            public bool stoneTools;
            public bool cropRotation;
            public bool organizedBuilding;
        }

        [SerializeField] private bool showDebugHud;
        private ResearchData data = new ResearchData();
        public bool StoneToolsUnlocked => data.stoneTools;
        public bool CropRotationUnlocked => data.cropRotation;
        public bool OrganizedBuildingUnlocked => data.organizedBuilding;
        public float ConstructionMultiplier => data.organizedBuilding ? 0.85f : 1f;
        public float FarmYieldMultiplier => (data.stoneTools ? 1.15f : 1f) * (data.cropRotation ? 1.5f : 1f);
        public string StatusMessage { get; private set; } = string.Empty;

        private void Awake()
        {
            Instance = this;
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<ResearchData>(json) ?? new ResearchData(); }
                catch (Exception exception)
                {
                    Debug.LogWarning("Research save could not be read. " + exception.Message);
                    data = new ResearchData();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool TryResearch(string researchId)
        {
            if (data == null)
                data = new ResearchData();
            var progression = SettlementProgression.Instance;
            if (progression != null && progression.NeedsCampfire)
                return Locked("Place your Campfire before researching.");
            var save = WorldSaveSystem.Instance;
            int food = 0, wood = 0, stone = 0;
            switch (researchId)
            {
                case "stone-tools":
                    if (data.stoneTools) return AlreadyKnown("Stone tools");
                    food = 10; wood = 15; stone = 10;
                    break;
                case "crop-rotation":
                    if (data.cropRotation) return AlreadyKnown("Crop rotation");
                    if (!data.stoneTools) return Locked("Research Stone Tools first.");
                    if (progression != null && progression.CampfireLevel < 2) return Locked("Crop Rotation unlocks at Campfire level 2.");
                    food = 25; wood = 15; stone = 10;
                    break;
                case "organized-building":
                    if (data.organizedBuilding) return AlreadyKnown("Organized building");
                    if (!data.stoneTools) return Locked("Research Stone Tools first.");
                    if (progression != null && progression.CampfireLevel < 3) return Locked("Organized Building unlocks at Campfire level 3.");
                    food = 20; wood = 30; stone = 25;
                    break;
                default:
                    return Locked("Unknown research.");
            }

            if (save == null || !save.TrySpendResources(food, wood, stone))
                return Locked("Not enough resources for this research.");

            if (researchId == "stone-tools") data.stoneTools = true;
            else if (researchId == "crop-rotation") data.cropRotation = true;
            else data.organizedBuilding = true;
            StatusMessage = "Research complete: " + GetName(researchId) + ".";
            Save();
            return true;
        }

        public string GetName(string researchId)
        {
            switch (researchId)
            {
                case "stone-tools": return "Stone Tools";
                case "crop-rotation": return "Crop Rotation";
                case "organized-building": return "Organized Building";
                default: return researchId;
            }
        }

        private bool AlreadyKnown(string name)
        {
            StatusMessage = name + " is already researched.";
            return false;
        }

        private bool Locked(string reason)
        {
            StatusMessage = reason;
            return false;
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            if (!showDebugHud) return;
            GUILayout.BeginArea(new Rect(Screen.width - 350f, 420f, 330f, 120f), GUI.skin.box);
            GUILayout.Label("RESEARCH");
            GUILayout.Label("Stone tools " + (data.stoneTools ? "✓" : "·") +
                "  Crop rotation " + (data.cropRotation ? "✓" : "·"));
            GUILayout.Label(StatusMessage);
            GUILayout.EndArea();
        }
    }
}
