#if UNITY_EDITOR
using System.IO;
using Founders.Seasons;
using Founders.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Founders.EditorTools
{
    internal static class StarterSceneBuilder
    {
        private const string SceneFolder = "Assets/Founders/Scenes";
        private const string ScenePath = SceneFolder + "/PrehistoricWorld.unity";
        private const string GeneratedFolder = "Assets/Founders/Generated";
        private const string CatalogPath = GeneratedFolder + "/WorldAssetCatalog.asset";
        private const string SettingsPath = GeneratedFolder + "/WorldGenerationSettings.asset";
        private const string SeasonPath = GeneratedFolder + "/PrehistoricSeason.asset";

        [MenuItem("Founders/World/Create Prehistoric Starter Scene")]
        private static void CreateScene()
        {
            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(GeneratedFolder);
            AssetDatabase.Refresh();

            WorldAssetCatalogBuilder.BuildCatalog();
            WorldAssetCatalogBuilder.CreateSettings();

            var catalog = AssetDatabase.LoadAssetAtPath<WorldAssetCatalog>(CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(SettingsPath);
            var season = EnsurePrehistoricSeason();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var world = new GameObject("World Streamer");
            var streamer = world.AddComponent<WorldStreamer>();
            var serializedStreamer = new SerializedObject(streamer);
            serializedStreamer.FindProperty("settings").objectReferenceValue = settings;
            serializedStreamer.FindProperty("assetCatalog").objectReferenceValue = catalog;
            serializedStreamer.ApplyModifiedPropertiesWithoutUndo();

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.name = "Main Camera";
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2200f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transform.position = new Vector3(0f, 330f, -430f);
            camera.transform.LookAt(Vector3.zero);
            camera.gameObject.AddComponent<MobileMapCamera>();

            var seasonDirector = world.AddComponent<SeasonDirector>();
            var serializedSeason = new SerializedObject(seasonDirector);
            serializedSeason.FindProperty("season").objectReferenceValue = season;
            serializedSeason.ApplyModifiedPropertiesWithoutUndo();

            var placer = world.AddComponent<BuildingPlacementController>();
            var serializedPlacer = new SerializedObject(placer);
            serializedPlacer.FindProperty("assetCatalog").objectReferenceValue = catalog;
            serializedPlacer.FindProperty("seasonDirector").objectReferenceValue = seasonDirector;
            serializedPlacer.FindProperty("mapCamera").objectReferenceValue = camera;
            serializedPlacer.ApplyModifiedPropertiesWithoutUndo();

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.intensity = 1.1f;
                light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }

            RenderSettings.ambientLight = new Color(0.58f, 0.62f, 0.59f);
            if (catalog != null && catalog.skyMaterials != null && catalog.skyMaterials.Length > 0)
                RenderSettings.skybox = catalog.skyMaterials[0];
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = world;

            Debug.Log("Created " + ScenePath + ". The world streams a 16.384km square region; inspect the asset catalog and tune device performance before release.");
        }

        private static SeasonDefinition EnsurePrehistoricSeason()
        {
            var season = AssetDatabase.LoadAssetAtPath<SeasonDefinition>(SeasonPath);
            if (season != null)
                return season;

            season = ScriptableObject.CreateInstance<SeasonDefinition>();
            season.seasonId = "season-01-prehistoric";
            season.displayName = "Prehistoric Civilization";
            season.eraLabel = "Stone Age";
            season.durationDays = 56;
            season.introduction = "Begin with a small camp and grow a thriving prehistoric settlement.";
            season.events = new[]
            {
                new SeasonEventDefinition
                {
                    eventId = "first-settlement",
                    title = "Found the settlement",
                    description = "Place three buildings to establish the first village.",
                    objective = SeasonObjective.PlaceBuildings,
                    startsOnDay = 0,
                    durationDays = 7,
                    progressGoal = 3,
                    rewardLabel = "Season points",
                    rewardAmount = 100
                },
                new SeasonEventDefinition
                {
                    eventId = "stone-age-tools",
                    title = "Stone for survival",
                    description = "Gather stone for the next stage of the settlement.",
                    objective = SeasonObjective.GatherStone,
                    startsOnDay = 7,
                    durationDays = 14,
                    progressGoal = 150,
                    rewardLabel = "Season points",
                    rewardAmount = 150
                },
                new SeasonEventDefinition
                {
                    eventId = "winter-stores",
                    title = "Prepare the stores",
                    description = "Gather food before the cold season.",
                    objective = SeasonObjective.GatherFood,
                    startsOnDay = 21,
                    durationDays = 14,
                    progressGoal = 250,
                    rewardLabel = "Season points",
                    rewardAmount = 200
                }
            };

            AssetDatabase.CreateAsset(season, SeasonPath);
            AssetDatabase.SaveAssets();
            return season;
        }
    }
}
#endif
