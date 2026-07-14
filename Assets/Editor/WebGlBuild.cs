using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Agrovator.PitchSimulator.Editor
{
    public static class WebGlBuild
    {
        public const string OutputDirectory = "Build/WebGL";
        public const string TemplateName = "PROJECT:Agrovator";

        private static readonly string[] CanonicalScenes =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Game.unity",
        };

        public static void BuildDevelopment()
        {
            ValidateScenes(EditorBuildSettings.scenes);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new BuildFailedException("Could not switch the active build target to WebGL.");
            }

            ApplyDevelopmentSettings();
            PrepareOutputDirectory();

            var report = BuildPipeline.BuildPlayer(CreateBuildOptions());
            var summary = report.summary;
            Debug.Log($"WebGL BuildReport: result={summary.result}; totalSize={summary.totalSize}; " +
                      $"totalTime={summary.totalTime}; warnings={summary.totalWarnings}; errors={summary.totalErrors}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL development build did not succeed. Result: {summary.result}.");
            }

            var indexPath = Path.Combine(GetProjectRoot(), OutputDirectory, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new BuildFailedException($"WebGL build entry point is missing at '{indexPath}'.");
            }
        }

        public static void ValidateScenes(EditorBuildSettingsScene[] scenes)
        {
            if (scenes == null || scenes.Length != CanonicalScenes.Length)
            {
                throw new BuildFailedException(
                    "Build Settings must contain exactly Bootstrap and Game, in that order.");
            }

            for (var index = 0; index < CanonicalScenes.Length; index++)
            {
                var scene = scenes[index];
                if (scene == null || !scene.enabled ||
                    !string.Equals(scene.path, CanonicalScenes[index], StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        "Build Settings must contain enabled Bootstrap then enabled Game, with no other scenes.");
                }

                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                {
                    throw new BuildFailedException($"Required build scene is missing: '{scene.path}'.");
                }
            }
        }

        public static BuildPlayerOptions CreateBuildOptions()
        {
            return new BuildPlayerOptions
            {
                scenes = (string[])CanonicalScenes.Clone(),
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.Development,
            };
        }

        public static void ApplyDevelopmentSettings()
        {
            PlayerSettings.companyName = "AGROVATOR";
            PlayerSettings.productName = "Pitch Simulator";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            AssetDatabase.SaveAssets();
        }

        private static void PrepareOutputDirectory()
        {
            var projectRoot = GetProjectRoot();
            var outputPath = Path.GetFullPath(Path.Combine(projectRoot, OutputDirectory));
            var expectedPath = Path.GetFullPath(Path.Combine(projectRoot, "Build", "WebGL"));
            if (!string.Equals(outputPath, expectedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException($"Refusing to clean unexpected build path '{outputPath}'.");
            }

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
            Directory.CreateDirectory(outputPath);
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}
