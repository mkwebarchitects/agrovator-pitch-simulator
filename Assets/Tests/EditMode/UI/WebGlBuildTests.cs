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
