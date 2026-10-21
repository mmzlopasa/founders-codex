using System;
using UnityEngine;

namespace Founders.Seasons
{
    public enum SeasonObjective
    {
        PlaceBuildings,
        GatherFood,
        GatherWood,
        GatherStone
    }

    [Serializable]
    public sealed class SeasonEventDefinition
    {
        public string eventId;
        public string title;
        [TextArea] public string description;
        public SeasonObjective objective;
        [Min(0)] public int startsOnDay;
        [Min(1)] public int durationDays = 7;
        [Min(1)] public int progressGoal = 5;
        public string rewardLabel = "Season points";
        [Min(0)] public int rewardAmount = 100;
    }

    [CreateAssetMenu(menuName = "Founders/Season Definition", fileName = "SeasonDefinition")]
    public sealed class SeasonDefinition : ScriptableObject
    {
        public string seasonId = "season-01-prehistoric";
        public string displayName = "Prehistoric Civilization";
        public string eraLabel = "Stone Age";
        [Min(1)] public int durationDays = 56;
        [TextArea] public string introduction;
        public SeasonEventDefinition[] events = Array.Empty<SeasonEventDefinition>();
    }
}
