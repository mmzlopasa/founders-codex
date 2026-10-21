using System;
using System.Collections.Generic;
using Founders.World;
using UnityEngine;

namespace Founders.Seasons
{
    /// <summary>
    /// Local first-season clock and event progress. Data is season-configurable so
    /// later eras can use the same UI and objective pipeline.
    /// </summary>
    public sealed class SeasonDirector : MonoBehaviour
    {
        [SerializeField] private SeasonDefinition season;
        [SerializeField] private bool showHud = true;

        private DateTime seasonStartUtc;
        private readonly Dictionary<string, int> progressByEvent = new Dictionary<string, int>();
        private readonly HashSet<string> completedEvents = new HashSet<string>();

        public SeasonDefinition ActiveSeason => season;
        public int SeasonDay => Mathf.Max(0, (int)(DateTime.UtcNow - seasonStartUtc).TotalDays);

        private string StartKey => "Founders.Season." + (season != null ? season.seasonId : "none") + ".StartUtc";

        private void Start()
        {
            if (season == null)
                return;

            if (long.TryParse(PlayerPrefs.GetString(StartKey, string.Empty), out var ticks) &&
                ticks > DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks)
            {
                seasonStartUtc = new DateTime(ticks, DateTimeKind.Utc);
            }
            else
            {
                seasonStartUtc = DateTime.UtcNow;
                PlayerPrefs.SetString(StartKey, seasonStartUtc.Ticks.ToString());
                PlayerPrefs.Save();
            }

            LoadEventProgress();
        }

        public void ReportProgress(SeasonObjective objective, int amount = 1)
        {
            if (season == null || amount <= 0 || SeasonDay >= season.durationDays)
                return;

            foreach (var eventDefinition in season.events)
            {
                if (eventDefinition == null || eventDefinition.objective != objective || !IsEventActive(eventDefinition))
                    continue;

                var key = ProgressKey(eventDefinition);
                var progress = GetProgress(eventDefinition) + amount;
                progress = Mathf.Min(progress, eventDefinition.progressGoal);
                progressByEvent[key] = progress;
                PlayerPrefs.SetInt(key, progress);

                if (progress >= eventDefinition.progressGoal)
                {
                    completedEvents.Add(key);
                    PlayerPrefs.SetInt(CompletionKey(eventDefinition), 1);
                }
            }

            PlayerPrefs.Save();
        }

        public int GetProgress(SeasonEventDefinition eventDefinition)
        {
            if (eventDefinition == null)
                return 0;

            var key = ProgressKey(eventDefinition);
            if (progressByEvent.TryGetValue(key, out var progress))
                return progress;

            return PlayerPrefs.GetInt(key, 0);
        }

        public bool IsComplete(SeasonEventDefinition eventDefinition)
        {
            return eventDefinition != null && completedEvents.Contains(ProgressKey(eventDefinition));
        }

        private bool IsEventActive(SeasonEventDefinition definition)
        {
            var dayInSeason = SeasonDay;
            return dayInSeason >= definition.startsOnDay &&
                   dayInSeason < definition.startsOnDay + definition.durationDays &&
                   !IsComplete(definition);
        }

        private void LoadEventProgress()
        {
            if (season.events == null)
                return;

            foreach (var eventDefinition in season.events)
            {
                if (eventDefinition == null)
                    continue;

                var progressKey = ProgressKey(eventDefinition);
                progressByEvent[progressKey] = PlayerPrefs.GetInt(progressKey, 0);
                if (PlayerPrefs.GetInt(CompletionKey(eventDefinition), 0) == 1)
                    completedEvents.Add(progressKey);
            }
        }

        private string ProgressKey(SeasonEventDefinition definition)
        {
            return "Founders.Event." + season.seasonId + "." + definition.eventId + ".Progress";
        }

        private string CompletionKey(SeasonEventDefinition definition)
        {
            return "Founders.Event." + season.seasonId + "." + definition.eventId + ".Complete";
        }

        private void OnGUI()
        {
            if (!showHud || season == null)
                return;

            GUILayout.BeginArea(new Rect(Screen.width - 350f, 16f, 334f, 164f), GUI.skin.box);
            GUILayout.Label(season.displayName + " · " + season.eraLabel);
            GUILayout.Label("Season day " + (SeasonDay + 1) + " / " + season.durationDays);

            var activeCount = 0;
            if (season.events != null)
            {
                foreach (var eventDefinition in season.events)
                {
                    if (eventDefinition == null || !IsEventActive(eventDefinition))
                        continue;

                    activeCount++;
                    GUILayout.Label(eventDefinition.title);
                    GUILayout.Label(
                        GetProgress(eventDefinition) + " / " + eventDefinition.progressGoal +
                        " · " + eventDefinition.rewardAmount + " " + eventDefinition.rewardLabel);
                    break;
                }
            }

            if (activeCount == 0)
                GUILayout.Label(SeasonDay >= season.durationDays ? "Season complete" : "No active event");

            GUILayout.EndArea();
        }
    }
}
