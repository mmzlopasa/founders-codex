using System;
using System.Collections.Generic;
using Founders.Seasons;
using UnityEngine;

namespace Founders.World
{
    public enum ResidentJob
    {
        Unassigned,
        Lumberjack,
        Miner,
        Farmer,
        Rancher,
        Builder,
        Hunter,
        Caregiver
    }

    public enum ResidentLifeStage
    {
        Child,
        Adult,
        Elder
    }

    [Serializable]
    public sealed class ResidentRecord
    {
        public string id;
        public string displayName;
        public int ageDays;
        public ResidentJob job;
        public int jobExperience;
        public int jobLevel = 1;
        public string mentorResidentId;
        public string parentAId;
        public string parentBId;

        public ResidentLifeStage LifeStage
        {
            get
            {
                if (ageDays < ResidentRoster.AdultAgeDays)
                    return ResidentLifeStage.Child;
                return ageDays >= ResidentRoster.ElderAgeDays
                    ? ResidentLifeStage.Elder
                    : ResidentLifeStage.Adult;
            }
        }
    }

    [Serializable]
    public sealed class ResidentHousehold
    {
        public string adultAId;
        public string adultBId;
        public List<string> childIds = new List<string>();
    }

    /// <summary>
    /// Persistent settlement population simulation. Work roles produce resources
    /// and gain experience. Children learn from a household mentor and inherit
    /// that mentor's vocation when they reach adulthood.
    /// </summary>
    public sealed class ResidentRoster : MonoBehaviour
    {
        public const int AdultAgeDays = 18 * 360;
        public const int ElderAgeDays = 60 * 360;

        private const string SaveKey = "Founders.Residents.v1";
        private static readonly string[] StartingNames =
        {
            "Ayla", "Kelan", "Mira", "Tarek"
        };
        private static readonly string[] ChildNames =
        {
            "Nima", "Oren", "Luma", "Soren", "Tala", "Eren", "Mavi", "Runa"
        };

        [SerializeField] private SeasonDirector seasonDirector;
        [SerializeField, Min(2f)] private float secondsPerGameDay = 15f;
        [SerializeField, Min(1)] private int populationPerHome = 4;
        [SerializeField] private bool showRoster = true;

        [Serializable]
        private sealed class RosterData
        {
            public long gameDay;
            public int farmingBoostDaysRemaining;
            public int nextResidentNumber = 5;
            public List<ResidentRecord> residents = new List<ResidentRecord>();
            public List<ResidentHousehold> households = new List<ResidentHousehold>();
        }

        public static ResidentRoster Instance { get; private set; }

        private RosterData data = new RosterData();
        private float dayTimer;
        private Vector2 rosterScroll;
        private string selectedForHousehold = string.Empty;

        public IReadOnlyList<ResidentRecord> Residents => data.residents;
        public long GameDay => data.gameDay;
        public float ConstructionSpeedMultiplier
        {
            get
            {
                var builderCount = 0;
                var builderLevels = 0;
                foreach (var resident in data.residents)
                {
                    if (resident.LifeStage == ResidentLifeStage.Adult && resident.job == ResidentJob.Builder)
                    {
                        builderCount++;
                        builderLevels += resident.jobLevel;
                    }
                }

                return Mathf.Clamp(1f - builderCount * 0.12f - builderLevels * 0.025f, 0.45f, 1f);
            }
        }

        private void Awake()
        {
            Instance = this;
            Load();
            if (data.residents.Count == 0)
                CreateStartingPopulation();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            dayTimer += Time.unscaledDeltaTime;
            if (dayTimer < secondsPerGameDay)
                return;

            dayTimer -= secondsPerGameDay;
            SimulateGameDay();
        }

        public void CycleJob(string residentId)
        {
            var resident = FindResident(residentId);
            if (resident == null || resident.LifeStage == ResidentLifeStage.Child)
                return;

            var next = ((int)resident.job + 1) % Enum.GetValues(typeof(ResidentJob)).Length;
            resident.job = (ResidentJob)next;
            Save();
        }

        public void UseFarmingSpeedup()
        {
            var save = WorldSaveSystem.Instance;
            if (save == null || !save.TrySpendFarmingSpeedup())
                return;

            data.farmingBoostDaysRemaining = Mathf.Max(data.farmingBoostDaysRemaining, 5);
            Save();
        }

        public void FormFirstAvailableHousehold()
        {
            for (var i = 0; i < data.residents.Count; i++)
            {
                if (!IsAdult(data.residents[i]) || IsInHousehold(data.residents[i].id))
                    continue;

                for (var j = i + 1; j < data.residents.Count; j++)
                {
                    if (!IsAdult(data.residents[j]) || IsInHousehold(data.residents[j].id))
                        continue;
                    FormHousehold(data.residents[i].id, data.residents[j].id);
                    return;
                }
            }
        }

        public void FormHousehold(string adultAId, string adultBId)
        {
            if (adultAId == adultBId || FindHousehold(adultAId, adultBId) != null)
                return;

            var adultA = FindResident(adultAId);
            var adultB = FindResident(adultBId);
            if (!IsAdult(adultA) || !IsAdult(adultB) ||
                IsInHousehold(adultAId) || IsInHousehold(adultBId))
                return;

            data.households.Add(new ResidentHousehold
            {
                adultAId = adultAId,
                adultBId = adultBId
            });
            Save();
        }

        private void SimulateGameDay()
        {
            data.gameDay++;
            foreach (var resident in data.residents)
            {
                resident.ageDays++;
                if (resident.LifeStage == ResidentLifeStage.Child)
                    LearnFromMentor(resident);
                else
                    WorkOneDay(resident);
            }

            if (data.farmingBoostDaysRemaining > 0)
                data.farmingBoostDaysRemaining--;

            if (data.gameDay % 30 == 0)
                GrowHouseholds();

            Save();
        }

        private void WorkOneDay(ResidentRecord resident)
        {
            var skillBonus = Mathf.Max(0, resident.jobLevel - 1);
            var food = 0;
            var wood = 0;
            var stone = 0;

            switch (resident.job)
            {
                case ResidentJob.Lumberjack:
                    wood = 2 + skillBonus;
                    break;
                case ResidentJob.Miner:
                    stone = 2 + skillBonus;
                    break;
                case ResidentJob.Farmer:
                    food = 3 + skillBonus;
                    break;
                case ResidentJob.Rancher:
                    food = 2 + skillBonus;
                    break;
                case ResidentJob.Hunter:
                    food = 2 + skillBonus;
                    break;
            }

            if (data.farmingBoostDaysRemaining > 0 &&
                (resident.job == ResidentJob.Farmer || resident.job == ResidentJob.Rancher))
                food *= 2;

            if (food + wood + stone > 0)
            {
                var save = WorldSaveSystem.Instance;
                if (save != null)
                    save.AddResources(food, wood, stone);

                if (seasonDirector != null)
                {
                    if (food > 0) seasonDirector.ReportProgress(SeasonObjective.GatherFood, food);
                    if (wood > 0) seasonDirector.ReportProgress(SeasonObjective.GatherWood, wood);
                    if (stone > 0) seasonDirector.ReportProgress(SeasonObjective.GatherStone, stone);
                }

                resident.jobExperience += food + wood + stone;
                while (resident.jobExperience >= resident.jobLevel * 20 && resident.jobLevel < 10)
                {
                    resident.jobExperience -= resident.jobLevel * 20;
                    resident.jobLevel++;
                }
            }
        }

        private void LearnFromMentor(ResidentRecord child)
        {
            var mentor = FindResident(child.mentorResidentId);
            if (mentor == null)
                return;

            // Children learn a vocation through ordinary family mentoring.
            child.job = mentor.job;
            child.jobExperience += Mathf.Max(1, mentor.jobLevel);
            child.jobLevel = Mathf.Clamp(1 + child.jobExperience / 120, 1, 9);
        }

        private void GrowHouseholds()
        {
            var save = WorldSaveSystem.Instance;
            if (save == null)
                return;

            foreach (var household in data.households)
            {
                if (household.childIds == null)
                    household.childIds = new List<string>();
                if (household.childIds.Count >= 3)
                    continue;

                var adults = data.residents.Count;
                var homes = CountHomes(save.Buildings);
                if (homes <= 0 || adults >= homes * populationPerHome || save.Food < 10)
                    continue;

                var parentA = FindResident(household.adultAId);
                var parentB = FindResident(household.adultBId);
                if (!IsAdult(parentA) || !IsAdult(parentB))
                    continue;

                if (!save.TrySpendResources(10, 0, 0))
                    continue;

                var name = ChildNames[(int)(data.gameDay / 30 + data.residents.Count) % ChildNames.Length];
                var child = new ResidentRecord
                {
                    id = "resident-" + data.nextResidentNumber++,
                    displayName = name,
                    ageDays = 0,
                    parentAId = parentA.id,
                    parentBId = parentB.id,
                    mentorResidentId = ChooseMentor(parentA, parentB).id
                };
                data.residents.Add(child);
                household.childIds.Add(child.id);
            }
        }

        private static ResidentRecord ChooseMentor(ResidentRecord first, ResidentRecord second)
        {
            return first.jobLevel >= second.jobLevel ? first : second;
        }

        private static int CountHomes(IReadOnlyList<WorldSaveSystem.PlacedBuilding> buildings)
        {
            var count = 0;
            foreach (var building in buildings)
            {
                if (building == null || string.IsNullOrEmpty(building.prefabName))
                    continue;

                var name = building.prefabName.ToLowerInvariant();
                if (name.Contains("hut") || name.Contains("house") || name.Contains("home") ||
                    name.Contains("tent") || name.Contains("longhouse"))
                    count++;
            }

            return count;
        }

        private ResidentRecord FindResident(string id)
        {
            return data.residents.Find(resident => resident.id == id);
        }

        private ResidentHousehold FindHousehold(string firstId, string secondId)
        {
            return data.households.Find(h =>
                (h.adultAId == firstId && h.adultBId == secondId) ||
                (h.adultAId == secondId && h.adultBId == firstId));
        }

        private bool IsInHousehold(string residentId)
        {
            return data.households.Exists(h => h.adultAId == residentId || h.adultBId == residentId);
        }

        private static bool IsAdult(ResidentRecord resident)
        {
            return resident != null && resident.LifeStage == ResidentLifeStage.Adult;
        }

        private void CreateStartingPopulation()
        {
            for (var i = 0; i < StartingNames.Length; i++)
            {
                data.residents.Add(new ResidentRecord
                {
                    id = "resident-" + (i + 1),
                    displayName = StartingNames[i],
                    ageDays = (22 + i * 3) * 360,
                    job = (ResidentJob)(1 + i % 4),
                    jobLevel = 1
                });
            }

            data.nextResidentNumber = StartingNames.Length + 1;
            data.households.Add(new ResidentHousehold
            {
                adultAId = data.residents[0].id,
                adultBId = data.residents[1].id
            });
            Save();
        }

        private void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    data = JsonUtility.FromJson<RosterData>(json) ?? new RosterData();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Resident save could not be read. " + exception.Message);
                    data = new RosterData();
                }
            }

            if (data.residents == null)
                data.residents = new List<ResidentRecord>();
            if (data.households == null)
                data.households = new List<ResidentHousehold>();
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Save();
        }

        private void OnGUI()
        {
            if (!showRoster)
                return;

            GUILayout.BeginArea(new Rect(20f, 18f, 380f, 385f), GUI.skin.box);
            GUILayout.Label("SETTLEMENT · DAY " + data.gameDay);
            GUILayout.Label("Residents " + data.residents.Count + " · Households " + data.households.Count);
            var speedups = WorldSaveSystem.Instance != null ? WorldSaveSystem.Instance.FarmingSpeedups : 0;
            if (GUILayout.Button("Farm boost · " + speedups + " available · " + data.farmingBoostDaysRemaining + " days active"))
                UseFarmingSpeedup();
            GUILayout.Label("Assign jobs with the job button. Pair two adults to form a household.");

            rosterScroll = GUILayout.BeginScrollView(rosterScroll);
            foreach (var resident in data.residents)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(resident.displayName + " · " + resident.LifeStage, GUILayout.Width(165f));
                if (resident.LifeStage == ResidentLifeStage.Child)
                {
                    GUILayout.Label("Learning " + resident.job);
                }
                else if (GUILayout.Button(resident.job + " Lv." + resident.jobLevel, GUILayout.Width(145f)))
                {
                    CycleJob(resident.id);
                }

                if (resident.LifeStage == ResidentLifeStage.Adult && !IsInHousehold(resident.id))
                {
                    if (GUILayout.Button("Pair", GUILayout.Width(48f)))
                    {
                        if (string.IsNullOrEmpty(selectedForHousehold))
                            selectedForHousehold = resident.id;
                        else
                        {
                            FormHousehold(selectedForHousehold, resident.id);
                            selectedForHousehold = string.Empty;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
