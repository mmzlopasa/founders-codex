#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Founders.EditorTools
{
    /// <summary>
    /// Entry point for a remote Unity build runner. Generates the scene/catalog from
    /// imported project assets, then exports an Xcode project for the iOS stage.
    /// </summary>
    public static class IOSBuildCommand
    {
        public static void BuildPlayer()
        {
            StarterSceneBuilder.CreateScene();

            PlayerSettings.productName = "Founders: The Long Rise";
            PlayerSettings.companyName = "Founders";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.iOS,
                "com.mmzlopasa.founderslongrise");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            var scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenePaths.Length == 0)
                throw new InvalidOperationException("The iOS build has no enabled scenes.");

            var options = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = "Build/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    "Founders iOS project export failed: " +
                    report.summary.result + " (" + report.summary.totalErrors + " errors).");
            }

            Debug.Log("iOS Xcode project exported to " + options.locationPathName);
        }
    }
}
#endif
