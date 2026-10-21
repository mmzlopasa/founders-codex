#if UNITY_EDITOR
using System.IO;
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

            var light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                light.intensity = 1.1f;
                light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }

            RenderSettings.ambientLight = new Color(0.58f, 0.62f, 0.59f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = world;

            Debug.Log("Created " + ScenePath + ". The world streams a 16.384km square region; inspect the asset catalog and tune device performance before release.");
        }
    }
}
#endif
