using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Agrovator.PitchSimulator.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class WebGlBuildTests
    {
        private static readonly string[] ExpectedScenes =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Game.unity",
        };

        [Test]
        public void ValidateScenes_AcceptsOnlyExactEnabledOrder()
        {
            Assert.DoesNotThrow(() => WebGlBuild.ValidateScenes(CreateScenes(ExpectedScenes, true)));

            Assert.Throws<BuildFailedException>(() => WebGlBuild.ValidateScenes(
                CreateScenes(new[] { ExpectedScenes[1], ExpectedScenes[0] }, true)));
            Assert.Throws<BuildFailedException>(() => WebGlBuild.ValidateScenes(
                CreateScenes(ExpectedScenes, true, false)));
            Assert.Throws<BuildFailedException>(() => WebGlBuild.ValidateScenes(
                CreateScenes(new[] { ExpectedScenes[0] }, true)));
            Assert.Throws<BuildFailedException>(() => WebGlBuild.ValidateScenes(
                CreateScenes(ExpectedScenes.Concat(new[] { "Assets/Scenes/WebIntegrationTest.unity" }).ToArray(), true)));
        }

        [Test]
        public void BuildOptions_AreDevelopmentWebGlAndUseCanonicalOutput()
        {
            var options = WebGlBuild.CreateBuildOptions();

            Assert.That(options.target, Is.EqualTo(BuildTarget.WebGL));
            Assert.That(options.locationPathName.Replace('\\', '/'), Is.EqualTo("Build/WebGL"));
            Assert.That(options.scenes, Is.EqualTo(ExpectedScenes));
            Assert.That(options.options.HasFlag(BuildOptions.Development), Is.True);
        }

        [Test]
        public void ApplyDevelopmentSettings_PersistsIntentionalWebGlContract()
        {
            WebGlBuild.ApplyDevelopmentSettings();

            Assert.That(PlayerSettings.companyName, Is.EqualTo("AGROVATOR"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Pitch Simulator"));
            Assert.That(PlayerSettings.defaultWebScreenWidth, Is.EqualTo(1280));
            Assert.That(PlayerSettings.defaultWebScreenHeight, Is.EqualTo(720));
            Assert.That(PlayerSettings.WebGL.template, Is.EqualTo("PROJECT:Agrovator"));
            Assert.That(PlayerSettings.WebGL.exceptionSupport, Is.EqualTo(WebGLExceptionSupport.FullWithStacktrace));
            Assert.That(PlayerSettings.WebGL.compressionFormat, Is.EqualTo(WebGLCompressionFormat.Gzip));
            Assert.That(PlayerSettings.WebGL.decompressionFallback, Is.True);
            Assert.That(PlayerSettings.WebGL.threadsSupport, Is.False);
            Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.WebGL), Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.GetStackTraceLogType(LogType.Exception), Is.EqualTo(StackTraceLogType.Full));

            // The release path moves this, so the development path has to put
            // it back or a development build silently inherits release codegen
            // from whichever build ran last.
            Assert.That(PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL),
                Is.EqualTo(Il2CppCodeGeneration.OptimizeSpeed));
        }

        /// <summary>
        /// Every deploy ships the same asset filenames, and Unity's loader keys its
        /// IndexedDB copies of the data and wasm on those URLs plus a product
        /// version that never changes. A returning learner therefore replays a
        /// stale build against a fresh framework and the player fails to start,
        /// which no amount of refreshing fixes. The stamp must move with content.
        /// </summary>
        [Test]
        public void CacheBusting_StampsEveryDownloadedAssetUrlWithTheBuildContentStamp()
        {
            const string html = @"
      const loaderUrl = `${buildUrl}/WebGL.loader.js`;
        dataUrl: `${buildUrl}/WebGL.data`,
        frameworkUrl: `${buildUrl}/WebGL.framework.js`,
        codeUrl: `${buildUrl}/WebGL.wasm`,
        streamingAssetsUrl: ""StreamingAssets"",";

            var stamped = WebGlBuild.ApplyCacheBusting(html, "abc123");

            StringAssert.Contains("/WebGL.loader.js?v=abc123`", stamped);
            StringAssert.Contains("/WebGL.data?v=abc123`", stamped);
            StringAssert.Contains("/WebGL.framework.js?v=abc123`", stamped);
            StringAssert.Contains("/WebGL.wasm?v=abc123`", stamped);
            Assert.That(stamped, Does.Not.Contain("StreamingAssets?v="),
                "Only the loader-downloaded build assets carry the stamp.");
            Assert.That(Regex.Matches(stamped, @"\?v=abc123").Count, Is.EqualTo(4));
        }

        /// <summary>
        /// A compressed release build serves `WebGL.wasm.br`, not `WebGL.wasm`,
        /// and Unity substitutes those real filenames into the template. A
        /// stamper that only knows the four uncompressed names matches nothing
        /// and silently no-ops, shipping a release with no cache busting at all
        /// - the exact failure the stamp exists to prevent, and one that leaves
        /// no trace in the build log.
        /// </summary>
        [TestCase(".unityweb")]
        [TestCase(".br")]
        [TestCase(".gz")]
        public void CacheBusting_StampsCompressedAssetUrlsWithoutLosingTheSuffix(string suffix)
        {
            var html = $@"
      const loaderUrl = `${{buildUrl}}/WebGL.loader.js{suffix}`;
        dataUrl: `${{buildUrl}}/WebGL.data{suffix}`,
        frameworkUrl: `${{buildUrl}}/WebGL.framework.js{suffix}`,
        codeUrl: `${{buildUrl}}/WebGL.wasm{suffix}`,
        streamingAssetsUrl: ""StreamingAssets"",";

            var stamped = WebGlBuild.ApplyCacheBusting(html, "abc123");

            StringAssert.Contains($"/WebGL.loader.js{suffix}?v=abc123`", stamped);
            StringAssert.Contains($"/WebGL.data{suffix}?v=abc123`", stamped);
            StringAssert.Contains($"/WebGL.framework.js{suffix}?v=abc123`", stamped);
            StringAssert.Contains($"/WebGL.wasm{suffix}?v=abc123`", stamped);
            Assert.That(Regex.Matches(stamped, @"\?v=abc123").Count, Is.EqualTo(4),
                "Every compressed asset URL the loader downloads must carry the stamp.");
            Assert.That(stamped, Does.Not.Contain($"?v=abc123{suffix}"),
                "The stamp must follow the compressed extension, not replace it.");
        }

        /// <summary>
        /// A Brotli build with the decompression fallback enabled - the only
        /// combination GitHub Pages can serve - writes `WebGL.wasm.unityweb`,
        /// not `WebGL.wasm.br`, and leaves `WebGL.loader.js` uncompressed. The
        /// stamp is derived from asset content, so it must hash the files that
        /// were actually produced; hashing only the names an uncompressed build
        /// happens to use would key every release on the loader alone and stop
        /// busting the cache when the wasm changes.
        /// </summary>
        [Test]
        public void ContentStampNames_CoverTheFilenamesAReleaseBuildActuallyWrites()
        {
            var names = WebGlBuild.StampedAssetNames().ToArray();

            foreach (var expected in new[]
                     {
                         "WebGL.loader.js", "WebGL.data.unityweb",
                         "WebGL.framework.js.unityweb", "WebGL.wasm.unityweb",
                     })
            {
                Assert.That(names, Contains.Item(expected),
                    expected + " is written by a fallback-compressed release build.");
            }

            foreach (var expected in new[]
                     {
                         "WebGL.data", "WebGL.framework.js", "WebGL.wasm",
                     })
            {
                Assert.That(names, Contains.Item(expected),
                    expected + " is written by the uncompressed development build.");
            }
        }

        [Test]
        public void ReleaseBuildOptions_CarryNoDevelopmentFlagAndUseCanonicalOutput()
        {
            var options = WebGlBuild.CreateReleaseBuildOptions();

            Assert.That(options.target, Is.EqualTo(BuildTarget.WebGL));
            Assert.That(options.locationPathName.Replace('\\', '/'), Is.EqualTo("Build/WebGL"));
            Assert.That(options.scenes, Is.EqualTo(ExpectedScenes));
            Assert.That(options.options.HasFlag(BuildOptions.Development), Is.False,
                "A release build must never ship the development player.");
            Assert.That(options.options.HasFlag(BuildOptions.AllowDebugging), Is.False);
        }

        /// <summary>
        /// The deployment target is GitHub Pages, which serves files verbatim
        /// and cannot add `Content-Encoding`. Compression is therefore only
        /// safe with Unity's decompression fallback, which names the assets
        /// `.br` and decompresses them in the loader.
        /// </summary>
        [Test]
        public void ApplyReleaseSettings_PersistsIntentionalReleaseWebGlContract()
        {
            try
            {
                WebGlBuild.ApplyReleaseSettings();

                Assert.That(PlayerSettings.companyName, Is.EqualTo("AGROVATOR"));
                Assert.That(PlayerSettings.productName, Is.EqualTo("Pitch Simulator"));
                Assert.That(PlayerSettings.defaultWebScreenWidth, Is.EqualTo(1280));
                Assert.That(PlayerSettings.defaultWebScreenHeight, Is.EqualTo(720));
                Assert.That(PlayerSettings.WebGL.template, Is.EqualTo("PROJECT:Agrovator"));
                Assert.That(PlayerSettings.WebGL.compressionFormat,
                    Is.EqualTo(WebGLCompressionFormat.Brotli));
                Assert.That(PlayerSettings.WebGL.decompressionFallback, Is.True,
                    "GitHub Pages cannot send Content-Encoding, so the loader must decompress.");
                Assert.That(PlayerSettings.WebGL.exceptionSupport,
                    Is.EqualTo(WebGLExceptionSupport.None));
                Assert.That(PlayerSettings.WebGL.threadsSupport, Is.False);
                Assert.That(PlayerSettings.GetScriptingBackend(NamedBuildTarget.WebGL),
                    Is.EqualTo(ScriptingImplementation.IL2CPP));
                Assert.That(PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL),
                    Is.EqualTo(Il2CppCodeGeneration.OptimizeSize));
                // Managed stripping is deliberately not asserted or set: a
                // development-only build from clean settings still serializes
                // managedStrippingLevel WebGL: 4, so Unity coerces WebGL to
                // High for every flavour and it cannot distinguish the two.
                Assert.That(PlayerSettings.GetStackTraceLogType(LogType.Exception),
                    Is.EqualTo(StackTraceLogType.None),
                    "A release build must not ship full stack traces to learners.");
            }
            finally
            {
                // Both suites and the smoke depend on the development output
                // shape, so this fixture must never leave release settings behind.
                WebGlBuild.ApplyDevelopmentSettings();
            }
        }

        [Test]
        public void CacheBusting_IsIdempotentAndRejectsAnEmptyStamp()
        {
            const string html = "dataUrl: `${buildUrl}/WebGL.data`,";

            var once = WebGlBuild.ApplyCacheBusting(html, "stamp1");
            var twice = WebGlBuild.ApplyCacheBusting(once, "stamp1");

            Assert.That(twice, Is.EqualTo(once),
                "Re-stamping an already stamped document must not append a second query.");
            Assert.That(() => WebGlBuild.ApplyCacheBusting(html, " "),
                Throws.ArgumentException);
        }

        [Test]
        public void Template_UsesCurrentTokensResponsiveAccessibleCanvasAndNoAutoplay()
        {
            var html = File.ReadAllText("Assets/WebGLTemplates/Agrovator/index.html");
            var css = File.ReadAllText("Assets/WebGLTemplates/Agrovator/TemplateData/style.css");

            foreach (var token in new[]
                     {
                         "{{{ LOADER_FILENAME }}}", "{{{ DATA_FILENAME }}}",
                         "{{{ FRAMEWORK_FILENAME }}}", "{{{ CODE_FILENAME }}}",
                         "JSON.stringify(COMPANY_NAME)", "JSON.stringify(PRODUCT_NAME)",
                         "JSON.stringify(PRODUCT_VERSION)",
                     })
            {
                StringAssert.Contains(token, html);
            }

            StringAssert.Contains("role=\"status\"", html);
            StringAssert.Contains("aria-live=\"polite\"", html);
            Assert.That(Regex.IsMatch(html,
                "<div\\s+id=\\\"unity-warning\\\"[^>]*\\shidden(?:\\s|>)",
                RegexOptions.CultureInvariant), Is.True,
                "The empty assertive warning region must be hidden before the first banner.");
            StringAssert.Contains("ResizeObserver", html);
            StringAssert.Contains("orientationchange", html);
            StringAssert.Contains("visualViewport", html);
            StringAssert.Contains("config.devicePixelRatio = renderScale", html);
            StringAssert.Contains("#unity-canvas", css);
            StringAssert.Contains("width: 100%", css);
            StringAssert.Contains("height: 100%", css);
            StringAssert.DoesNotContain("aspect-ratio", css);
            StringAssert.DoesNotContain("<audio", html.ToLowerInvariant());
            StringAssert.DoesNotContain("autoplay", html.ToLowerInvariant());
            StringAssert.DoesNotContain("audiocontext", html.ToLowerInvariant());
            StringAssert.DoesNotContain("console.", html.ToLowerInvariant());
        }

        private static EditorBuildSettingsScene[] CreateScenes(string[] paths, bool enabled, params bool[] overrides)
        {
            return paths.Select((path, index) => new EditorBuildSettingsScene(
                path, index < overrides.Length ? overrides[index] : enabled)).ToArray();
        }
    }
}
