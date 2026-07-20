using System;
using System.IO;
using System.Linq;
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
            Build("development", ApplyDevelopmentSettings, CreateBuildOptions);
        }

        /// <summary>
        /// The build learners download. Compressed with Brotli behind Unity's
        /// decompression fallback because GitHub Pages serves files verbatim
        /// and cannot send <c>Content-Encoding</c>, with no development player,
        /// no exception stack traces, and IL2CPP told to optimize for size.
        /// </summary>
        public static void BuildRelease()
        {
            var releaseSettingsApplied = false;
            try
            {
                Build(
                    "release",
                    () =>
                    {
                        ApplyReleaseSettings();
                        releaseSettingsApplied = true;
                    },
                    CreateReleaseBuildOptions);
            }
            finally
            {
                // ProjectSettings.asset is version controlled, and the release
                // knobs persist through SaveAssets. Leaving them behind would
                // silently change the shape of the next development build and
                // of the smoke that depends on it. Only restore what this call
                // actually changed, so a build that failed before applying
                // settings does not rewrite them, and never let the restore
                // throw away the exception that explains the build failure.
                if (releaseSettingsApplied)
                {
                    try
                    {
                        ApplyDevelopmentSettings();
                    }
                    catch (Exception restoreFailure)
                    {
                        Debug.LogError(
                            "Failed to restore development player settings after a release " +
                            $"build: {restoreFailure}");
                    }
                }
            }
        }

        private static void Build(
            string label,
            Action applySettings,
            Func<BuildPlayerOptions> createOptions)
        {
            ValidateScenes(EditorBuildSettings.scenes);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new BuildFailedException("Could not switch the active build target to WebGL.");
            }

            applySettings();
            PrepareOutputDirectory();

            var report = BuildPipeline.BuildPlayer(createOptions());
            var summary = report.summary;
            Debug.Log($"WebGL BuildReport: result={summary.result}; totalSize={summary.totalSize}; " +
                      $"totalTime={summary.totalTime}; warnings={summary.totalWarnings}; errors={summary.totalErrors}.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL {label} build did not succeed. Result: {summary.result}.");
            }

            var indexPath = Path.Combine(GetProjectRoot(), OutputDirectory, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new BuildFailedException($"WebGL build entry point is missing at '{indexPath}'.");
            }

            StampBuildAssets(indexPath);
        }

        /// <summary>
        /// Every build ships the same asset filenames under a product version that
        /// never changes, and Unity's loader keeps its own IndexedDB copies of the
        /// data and wasm keyed on exactly that. Without a stamp a returning learner
        /// replays a cached build against a freshly downloaded framework, the
        /// player refuses to start, and refreshing cannot clear it. The stamp is
        /// derived from content, so an unchanged build keeps its cached assets.
        /// </summary>
        private static void StampBuildAssets(string indexPath)
        {
            var buildRoot = Path.Combine(Path.GetDirectoryName(indexPath), "Build");
            var stamp = ComputeContentStamp(buildRoot);
            var stamped = ApplyCacheBusting(File.ReadAllText(indexPath), stamp);

            // A compressed build serves different filenames, so a stamper that
            // silently matched nothing would ship an unbustable cache with a
            // reassuring log line. Fail the build instead.
            var stampCount = CountStampedAssets(stamped, stamp);
            if (stampCount != AssetNames.Length)
            {
                throw new BuildFailedException(
                    $"Cache busting stamped {stampCount} of {AssetNames.Length} loader asset URLs. " +
                    "The built filenames do not match the names the stamper knows.");
            }

            File.WriteAllText(indexPath, stamped);
            Debug.Log($"WebGL build assets stamped with cache-busting token '{stamp}'.");
        }

        /// <summary>
        /// The assets the loader downloads on every run. The template also
        /// emits a <c>symbolsUrl</c>, knowingly excluded: it exists only for
        /// builds that emit a symbols file, which the release player never
        /// does, so stamping it would gate the build on a file that is usually
        /// absent.
        /// </summary>
        private static readonly string[] AssetNames =
        {
            "WebGL.loader.js", "WebGL.data", "WebGL.framework.js", "WebGL.wasm",
        };

        /// <summary>
        /// Compression renames the downloaded assets, so every variant is
        /// hashed and every variant is stampable. Unity writes
        /// <c>.unityweb</c> - not <c>.br</c> or <c>.gz</c> - whenever the
        /// decompression fallback is on, which is the only configuration
        /// GitHub Pages can serve; the server-header variants are covered too
        /// so turning the fallback off later cannot silently unstamp a build.
        /// </summary>
        private static readonly string[] CompressionSuffixes =
        {
            string.Empty, ".unityweb", ".br", ".gz",
        };

        /// <summary>
        /// How many of the loader's assets carry this stamp. Counted per
        /// asset, not as total occurrences, so a template that referenced one
        /// asset twice could never compensate for another going unstamped.
        /// </summary>
        public static int CountStampedAssets(string html, string stamp)
        {
            if (html == null) throw new ArgumentNullException(nameof(html));
            if (string.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentException("A cache-busting stamp is required.", nameof(stamp));
            }

            return AssetNames.Count(asset => System.Text.RegularExpressions.Regex.IsMatch(
                html,
                "/" + System.Text.RegularExpressions.Regex.Escape(asset) +
                    @"(\.unityweb|\.br|\.gz)?\?v=" +
                    System.Text.RegularExpressions.Regex.Escape(stamp) + "`"));
        }

        /// <summary>
        /// Every filename any supported build flavour can produce.
        /// </summary>
        public static System.Collections.Generic.IEnumerable<string> StampedAssetNames()
        {
            return AssetNames.SelectMany(
                name => CompressionSuffixes.Select(suffix => name + suffix));
        }

        private static string ComputeContentStamp(string buildRoot)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var names = StampedAssetNames().ToArray();
                Array.Sort(names, StringComparer.Ordinal);
                var hashed = 0;
                foreach (var name in names)
                {
                    var path = Path.Combine(buildRoot, name);
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var bytes = File.ReadAllBytes(path);
                    sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
                    hashed++;
                }

                // Hashing nothing yields a constant, so every build would share
                // one stamp and none would bust a cache. Fail loudly instead.
                if (hashed == 0)
                {
                    throw new BuildFailedException(
                        $"No build assets were found under '{buildRoot}' to derive a " +
                        "cache-busting stamp from.");
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", string.Empty)
                    .Substring(0, 12).ToLowerInvariant();
            }
        }

        /// <summary>
        /// Appends the stamp to the four URLs the loader downloads. Idempotent, so
        /// re-stamping a document never produces a second query string.
        /// </summary>
        public static string ApplyCacheBusting(string html, string stamp)
        {
            if (string.IsNullOrWhiteSpace(stamp))
            {
                throw new ArgumentException("A cache-busting stamp is required.", nameof(stamp));
            }

            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            foreach (var asset in AssetNames)
            {
                // The optional (\.br|\.gz) group is what keeps a compressed
                // release build stamped; without it the match fails on the
                // real built filename and cache busting quietly disappears.
                html = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    "/" + System.Text.RegularExpressions.Regex.Escape(asset) +
                        @"(\.unityweb|\.br|\.gz)?(\?v=[^`""']*)?`",
                    "/" + asset + "$1?v=" + stamp + "`");
            }

            return html;
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

        public static BuildPlayerOptions CreateReleaseBuildOptions()
        {
            return new BuildPlayerOptions
            {
                scenes = (string[])CanonicalScenes.Clone(),
                locationPathName = OutputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };
        }

        /// <summary>
        /// Brotli plus <see cref="WebGLPlayerSettings.decompressionFallback"/>
        /// is the only compression that is correct on GitHub Pages, which
        /// serves the file bytes verbatim and cannot add a
        /// <c>Content-Encoding</c> header; the fallback makes Unity's loader
        /// decompress in the browser instead. Stack traces and the development
        /// player are off, and IL2CPP is told to optimize for size, because the
        /// target deployment is Malaysian school wifi.
        /// </summary>
        public static void ApplyReleaseSettings()
        {
            ApplySharedSettings();
            // Not None: None stops managed exceptions being caught at all, and
            // the safe-fallback screen is reached through catch blocks in
            // Bootstrapper, GuidedPitchContentLoader and WebGlLmsBridge. This
            // ships no stack traces while keeping that recovery path alive.
            PlayerSettings.WebGL.exceptionSupport =
                WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,
                Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
            AssetDatabase.SaveAssets();
        }

        private static void ApplySharedSettings()
        {
            PlayerSettings.companyName = "AGROVATOR";
            PlayerSettings.productName = "Pitch Simulator";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        }

        /// <summary>
        /// Every knob the release path changes is set back explicitly, so a
        /// development build after a release build is the same build it always
        /// was rather than whatever the last release left behind.
        /// </summary>
        public static void ApplyDevelopmentSettings()
        {
            ApplySharedSettings();
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,
                Il2CppCodeGeneration.OptimizeSpeed);
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
