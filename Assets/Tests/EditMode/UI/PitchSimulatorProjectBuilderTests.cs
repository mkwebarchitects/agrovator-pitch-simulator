using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.Audio;
using Agrovator.PitchSimulator.Editor;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.LMS;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    public sealed class PitchSimulatorProjectBuilderTests
    {
        private const string GamePath = "Assets/Scenes/Game.unity";
        private const string SentinelName = "Review Sentinel - Preserve Me";
        private const string GuidedContentPath = "Assets/Content/Scenarios/guided-pitch-builder.en.json";
        private const string ContentRoot = "Content Frame/Phase Scroll/Viewport/Content";
        private static readonly Vector2 WideSize = new Vector2(1280f, 720f);
        private static readonly Vector2[] CompactSizes =
        {
            new Vector2(960f, 720f),
            new Vector2(720f, 960f),
        };
        private static readonly Color DeepNavy = new Color32(0x0E, 0x17, 0x1F, 0xFF);
        private static readonly Color Cream = new Color32(0xF4, 0xEA, 0xD5, 0xFF);
        private static readonly Color FocusGold = new Color32(0xFF, 0xD1, 0x66, 0xFF);
        private static readonly string[] PhaseSectionNames =
        {
            "Mode Selection", "Learn", "Sentence Cards", "Feedback",
            "Improve Actions", "Present Pitch", "Follow Up",
        };
        private static readonly string[] GeneratedScenePaths =
        {
            "Assets/Scenes/Bootstrap.unity",
            GamePath,
            "Assets/Scenes/WebIntegrationTest.unity",
        };

        [Test]
        public void BatchBuild_Twice_PreservesUnownedRootAndKeepsGuidedContractSingular()
        {
            var originalSceneBytes = new Dictionary<string, byte[]>();
            foreach (var path in GeneratedScenePaths)
            {
                originalSceneBytes.Add(path, File.ReadAllBytes(path));
            }
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            var sentinel = new GameObject(SentinelName);
            SceneManager.MoveGameObjectToScene(sentinel, scene);
            EditorSceneManager.SaveScene(scene);

            try
            {
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                AssertGuidedGameContract(scene);
                AssertBootstrapContract();
                AssertWebIntegrationContract();
                AssertExactBuildSettings();
                var firstBuildBytes = GeneratedScenePaths.ToDictionary(
                    path => path, File.ReadAllBytes, StringComparer.Ordinal);
                var firstOwnedGameRoot = scene.GetRootGameObjects()
                    .Single(root => root.name == "Generated UI");
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                Assert.That(scene.GetRootGameObjects().Single(root => root.name == "Generated UI"),
                    Is.SameAs(firstOwnedGameRoot),
                    "An unchanged build must reuse the current owned root and its serialized IDs.");
                AssertGuidedGameContract(scene);
                AssertBootstrapContract();
                AssertWebIntegrationContract();
                AssertExactBuildSettings();
                foreach (var path in GeneratedScenePaths)
                {
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(firstBuildBytes[path]),
                        path + " must be byte-identical after a second unchanged builder run.");
                }

                var responsive = firstOwnedGameRoot
                    .GetComponentInChildren<GuidedPitchResponsiveLayout>(true);
                var responsiveSerialized = new SerializedObject(responsive);
                responsiveSerialized.FindProperty("modeSelectionControls").objectReferenceValue = null;
                responsiveSerialized.ApplyModifiedPropertiesWithoutUndo();
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                var responsiveMigratedRoot = scene.GetRootGameObjects()
                    .Single(root => root.name == "Generated UI");
                Assert.That(responsiveMigratedRoot,
                    Is.Not.SameAs(firstOwnedGameRoot),
                    "A partial responsive-reference contract must be rebuilt and migrated.");
                var migratedResponsive = responsiveMigratedRoot
                    .GetComponentInChildren<GuidedPitchResponsiveLayout>(true);
                Assert.That(migratedResponsive.ValidateContract(out var responsiveReason), Is.True,
                    responsiveReason);

                UnityEngine.Object.DestroyImmediate(
                    responsiveMigratedRoot.transform.Find("Canvas").gameObject);
                PitchSimulatorProjectBuilder.BuildProjectFoundationBatch();
                Assert.That(scene.GetRootGameObjects().Single(root => root.name == "Generated UI"),
                    Is.Not.SameAs(responsiveMigratedRoot),
                    "A structurally stale generated contract must still be rebuilt and migrated.");
                AssertGuidedGameContract(scene);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var pair in originalSceneBytes)
                {
                    File.WriteAllBytes(pair.Key, pair.Value);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }

        [Test]
        public void GeneratedGuidedPitch_WideLayout_MeetsApprovedFrameBoardAndCardContract()
        {
            var canvas = OpenGameCanvas(WideSize);
            var guided = RequireChild(canvas, "Guided Pitch");
            using (new ActiveScope(guided.gameObject))
            {
                ApplyResponsiveLayout(guided, WideSize);
                SetPhaseSections(guided, "Sentence Cards");
                ForceGuidedLayout(canvas, guided);

                var frame = RequireChild(guided, "Content Frame");
                var frameRect = frame.GetComponent<RectTransform>();
                var frameImage = frame.GetComponent<Image>();
                Assert.That(frameImage.color, Is.EqualTo(DeepNavy),
                    "The guided panel must be opaque deep navy.");
                Assert.That(frameRect.rect.width, Is.InRange(960f, 1000f));
                Assert.That(frameRect.rect.height, Is.LessThanOrEqualTo(680f));
                var guidedRect = guided.GetComponent<RectTransform>();
                var frameCenter = guidedRect.InverseTransformPoint(
                    frameRect.TransformPoint(frameRect.rect.center));
                Assert.That(frameCenter.x, Is.EqualTo(guidedRect.rect.center.x).Within(0.5f),
                    "The content frame must stay horizontally centered.");
                Assert.That(Mathf.Abs(frameCenter.y - guidedRect.rect.center.y), Is.LessThanOrEqualTo(1f),
                    "The content frame must stay vertically centered.");

                var environment = RequireChild(guided, "Environment Frame");
                var environmentRect = environment.GetComponent<RectTransform>();
                Assert.That(environmentRect.rect.width, Is.EqualTo(guidedRect.rect.width).Within(0.1f));
                Assert.That(environmentRect.rect.height, Is.EqualTo(guidedRect.rect.height).Within(0.1f));
                Assert.That(environmentRect.rect.width - frameRect.rect.width, Is.GreaterThanOrEqualTo(200f),
                    "The environment must remain visible outside the content panel.");
                Assert.That(environment.GetComponent<Image>().sprite.name, Is.EqualTo("pitch-room"));

                var board = RequireChild(frame, "Pitch Board");
                var boardGrid = board.GetComponent<GridLayoutGroup>();
                Assert.That(boardGrid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedRowCount));
                Assert.That(boardGrid.constraintCount, Is.EqualTo(1));
                var boardSlots = board.GetComponent<PitchBoardView>().Slots;
                Assert.That(boardSlots.Count, Is.EqualTo(4));
                var slotRects = boardSlots.Select(slot => slot.Root.GetComponent<RectTransform>()).ToArray();
                Assert.That(slotRects.Select(rect => rect.rect.size).Distinct().Count(), Is.EqualTo(1),
                    "The wide Pitch Board must render four equal slots.");
                Assert.That(slotRects.Select(rect => Mathf.Round(rect.position.y)).Distinct().Count(),
                    Is.EqualTo(1), "The wide Pitch Board must keep all four slots in one row.");

                var cardsSection = RequireChild(guided, ContentRoot + "/Sentence Cards");
                var cardViews = cardsSection.GetComponent<SentenceCardListView>().Cards;
                Assert.That(cardViews.Count, Is.EqualTo(3));
                var cardsRect = cardsSection.GetComponent<RectTransform>();
                foreach (var card in cardViews)
                {
                    var backing = card.transform.Find("Backing").GetComponent<Image>();
                    Assert.That(backing.color, Is.EqualTo(Cream),
                        card.name + " must remain a cream selectable card.");
                    AssertContained(cardsRect, (RectTransform)card.transform);
                }

                foreach (var section in new[] { "Sentence Cards", "Feedback" })
                {
                    SetPhaseSections(guided, section);
                    ForceGuidedLayout(canvas, guided);
                    foreach (var child in frame.GetComponentsInChildren<RectTransform>(false))
                    {
                        if (child == frameRect)
                        {
                            continue;
                        }
                        AssertContained(frameRect, child);
                    }
                }
                AssertContained(guidedRect, frameRect);
            }

            AssertNoLegacyPresentation(canvas);
        }

        [Test]
        public void GeneratedGuidedPitch_CompactFixtures_ReflowStackAndContainContent()
        {
            foreach (var size in CompactSizes)
            {
                var canvas = OpenGameCanvas(size);
                var guided = RequireChild(canvas, "Guided Pitch");
                using (new ActiveScope(guided.gameObject))
                {
                    var layout = guided.GetComponent<GuidedPitchResponsiveLayout>();
                    Assert.That(layout, Is.Not.Null,
                        "The guided panel must carry the serialized responsive layout.");
                    layout.Apply(size);
                    Assert.That(layout.IsCompact, Is.True, size + " must reflow to the compact layout.");
                    SetPhaseSections(guided, "Sentence Cards");
                    var frame = RequireChild(guided, "Content Frame");
                    var primaryAction = RequireChild(frame, "Primary Action");
                    ForceGuidedLayout(canvas, guided);

                    var frameRect = frame.GetComponent<RectTransform>();
                    Assert.That(frameRect.rect.width, Is.LessThanOrEqualTo(size.x - 48f),
                        size + " must keep 24px horizontal safe padding.");
                    Assert.That(frameRect.rect.height, Is.LessThanOrEqualTo(size.y - 48f),
                        size + " must keep 24px vertical safe padding.");

                    var board = RequireChild(frame, "Pitch Board");
                    var boardGrid = board.GetComponent<GridLayoutGroup>();
                    Assert.That(boardGrid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                    Assert.That(boardGrid.constraintCount, Is.EqualTo(2));
                    var slotRects = board.GetComponent<PitchBoardView>().Slots
                        .Select(slot => slot.Root.GetComponent<RectTransform>()).ToArray();
                    Assert.That(slotRects.Select(rect => Mathf.Round(rect.position.y)).Distinct().Count(),
                        Is.EqualTo(2), size + " must reflow the Pitch Board into two rows.");

                    var cardsGrid = RequireChild(guided, ContentRoot + "/Sentence Cards")
                        .GetComponent<GridLayoutGroup>();
                    Assert.That(cardsGrid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                    Assert.That(cardsGrid.constraintCount, Is.EqualTo(1),
                        size + " must stack sentence cards into a single column.");

                    var scroll = RequireChild(frame, "Phase Scroll").GetComponent<ScrollRect>();
                    Assert.That(scroll.enabled, Is.True, size + " must enable the compact Phase Scroll.");
                    var viewport = scroll.viewport;
                    var content = scroll.content;
                    var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                        viewport, content);
                    foreach (var descendant in content.GetComponentsInChildren<RectTransform>(false))
                    {
                        if (descendant == content)
                        {
                            continue;
                        }
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                            viewport, descendant);
                        Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(viewport.rect.xMin - 0.5f),
                            descendant.name + " escaped the Phase Scroll viewport on the left at " + size);
                        Assert.That(bounds.max.x, Is.LessThanOrEqualTo(viewport.rect.xMax + 0.5f),
                            descendant.name + " escaped the Phase Scroll viewport on the right at " + size);
                        Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(contentBounds.min.y - 0.5f),
                            descendant.name + " escaped the scrollable content at " + size);
                        Assert.That(bounds.max.y, Is.LessThanOrEqualTo(contentBounds.max.y + 0.5f),
                            descendant.name + " escaped the scrollable content at " + size);
                    }
                    foreach (var selectable in content.GetComponentsInChildren<Selectable>(false))
                    {
                        var rect = (RectTransform)selectable.transform;
                        Assert.That(rect.rect.height, Is.LessThanOrEqualTo(viewport.rect.height + 0.5f),
                            selectable.name + " must be scrollable fully visible at " + size);
                    }

                    SetPhaseSections(guided, PhaseSectionNames);
                    foreach (Transform button in primaryAction)
                    {
                        button.gameObject.SetActive(true);
                    }
                    ForceGuidedLayout(canvas, guided);

                    foreach (var groupPath in new[] { "Mode Selection", "Improve Actions" })
                    {
                        var group = content.Find(groupPath).GetComponent<RectTransform>();
                        Assert.That(group.GetComponent<LayoutElement>(), Is.Null,
                            group.name + " must use its flow layout as the sole size authority.");
                        foreach (Transform child in group)
                        {
                            AssertContained(group, child.GetComponent<RectTransform>());
                        }
                    }
                    foreach (Transform activeGroup in content)
                    {
                        if (activeGroup.gameObject.activeSelf)
                        {
                            AssertContained(content, activeGroup.GetComponent<RectTransform>());
                        }
                    }
                    var primaryRect = primaryAction.GetComponent<RectTransform>();
                    Assert.That(primaryRect.GetComponent<LayoutElement>(), Is.Null,
                        "Primary Action must use its flow layout as the sole size authority.");
                    foreach (Transform child in primaryAction)
                    {
                        AssertContained(primaryRect, child.GetComponent<RectTransform>());
                    }
                }

                var results = RequireChild(canvas, "Results");
                using (new ActiveScope(results.gameObject))
                {
                    ForceResultsLayout(canvas, results);
                    var frameRect = RequireChild(results, "Content Frame").GetComponent<RectTransform>();
                    Assert.That(frameRect.rect.width, Is.LessThanOrEqualTo(size.x - 48f));
                    Assert.That(frameRect.rect.height, Is.LessThanOrEqualTo(size.y - 48f));
                    var partViews = results.GetComponentsInChildren<PitchResultPartView>(true);
                    Assert.That(partViews, Has.Length.EqualTo(4),
                        "Results must reuse the four part visuals.");
                    var ordered = partViews.OrderBy(view => (int)view.Part)
                        .Select(view => (RectTransform)view.transform).ToArray();
                    for (var index = 1; index < ordered.Length; index++)
                    {
                        Assert.That(ordered[index].position.y, Is.LessThan(ordered[index - 1].position.y),
                            "Results part cards must stack vertically on the constrained fixture.");
                    }
                }
            }
        }

        [Test]
        public void GeneratedScenes_RenderRepresentativeGuidedContentWithoutTruncation()
        {
            var fixture = LoadGuidedFixture();
            var failures = new List<string>();
            var sizes = new[] { WideSize }.Concat(CompactSizes).ToArray();
            foreach (var size in sizes)
            {
                var canvas = OpenGameCanvas(size);
                AssertGuidedCopyRenders(canvas, size, fixture, failures);
                AssertResultsCopyRenders(canvas, size, fixture, failures);
                AssertSupportScreenCopyRenders(canvas, size, fixture, failures);
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        [Test]
        public void GeneratedScenes_MeetTargetContrastNavigationAndPixelArtContracts()
        {
            var canvas = OpenGameCanvas(WideSize);
            foreach (var selectable in canvas.GetComponentsInChildren<Selectable>(true))
            {
                Assert.That(selectable.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit),
                    selectable.name + " must use explicit navigation.");
                var indicator = selectable.GetComponents<MonoBehaviour>()
                    .SingleOrDefault(component => component.GetType().Name == "SelectableFocusIndicator");
                Assert.That(indicator, Is.Not.Null,
                    selectable.name + " must own the reusable keyboard-focus indicator.");
                var outline = selectable.targetGraphic.GetComponent<Outline>();
                Assert.That(outline, Is.Not.Null, selectable.name + " must render a focus outline.");
                Assert.That(outline.effectColor, Is.EqualTo(FocusGold));
                Assert.That(ContrastRatio(outline.effectColor, DeepNavy), Is.GreaterThanOrEqualTo(3f),
                    selectable.name + " focus must contrast with the adjacent navy surface by 3:1.");
            }

            var targetFailures = new List<string>();
            foreach (var screenName in new[]
                { "Title", "Briefing", "Guided Pitch", "Results", "Settings" })
            {
                var screen = RequireChild(canvas, screenName);
                using (new ActiveScope(screen.gameObject))
                {
                    if (screenName == "Guided Pitch")
                    {
                        ApplyResponsiveLayout(screen, WideSize);
                        SetPhaseSections(screen, "Mode Selection", "Sentence Cards", "Improve Actions");
                        ForceGuidedLayout(canvas, screen);
                    }
                    else
                    {
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(screen.GetComponent<RectTransform>());
                        LayoutRebuilder.ForceRebuildLayoutImmediate(
                            RequireChild(screen, "Content Frame").GetComponent<RectTransform>());
                        Canvas.ForceUpdateCanvases();
                    }

                    foreach (var selectable in screen.GetComponentsInChildren<Selectable>(false))
                    {
                        var rect = (RectTransform)selectable.transform;
                        if (rect.rect.width < 64f || rect.rect.height < 64f)
                        {
                            targetFailures.Add(
                                $"{screenName}/{selectable.name} rendered {rect.rect.width:F1}x" +
                                $"{rect.rect.height:F1}px; expected a 64px minimum target.");
                        }
                    }
                }
            }
            Assert.That(targetFailures, Is.Empty, string.Join(Environment.NewLine, targetFailures));

            var contrastFailures = new List<string>();
            var guided = RequireChild(canvas, "Guided Pitch");
            var frameImage = RequireChild(guided, "Content Frame").GetComponent<Image>();
            var dialogueCard = RequireChild(guided, "Content Frame/Aya Row/Dialogue Card");
            CheckContrast("dialogue question", RequireText(dialogueCard, "Question"),
                dialogueCard.GetComponent<Image>(), 4.5f, contrastFailures);
            CheckContrast("dialogue hint", RequireText(dialogueCard, "Hint"),
                dialogueCard.GetComponent<Image>(), 4.5f, contrastFailures);
            var boardSlot = RequireChild(guided, "Content Frame/Pitch Board/Problem Slot");
            CheckContrast("board label", RequireText(boardSlot, "Header/Label"),
                boardSlot.Find("Backing").GetComponent<Image>(), 4.5f, contrastFailures);
            CheckContrast("board sentence", RequireText(boardSlot, "Sentence"),
                boardSlot.Find("Backing").GetComponent<Image>(), 4.5f, contrastFailures);
            var sentenceCard = RequireChild(guided, ContentRoot + "/Sentence Cards/Card 1");
            CheckContrast("sentence card label", RequireText(sentenceCard, "Label"),
                sentenceCard.Find("Backing").GetComponent<Image>(), 4.5f, contrastFailures);
            var feedbackRow = RequireChild(guided, ContentRoot + "/Feedback/Row 1");
            CheckContrast("feedback label", RequireText(feedbackRow, "Label"),
                feedbackRow.GetComponent<Image>(), 4.5f, contrastFailures);
            CheckContrast("feedback value", RequireText(feedbackRow, "Value"),
                feedbackRow.GetComponent<Image>(), 4.5f, contrastFailures);
            var strengthen = RequireChild(guided, ContentRoot + "/Improve Actions/Strengthen Problem");
            CheckContrast("strengthen label", RequireText(strengthen, "Label"),
                strengthen.GetComponent<Image>(), 4.5f, contrastFailures);
            var continueButton = RequireChild(guided, "Content Frame/Primary Action/Continue Button");
            CheckContrast("continue label", RequireText(continueButton, "Label"),
                continueButton.GetComponent<Image>(), 4.5f, contrastFailures);
            var railSlot = RequireChild(guided, "Content Frame/Progress Rail/Problem Slot");
            CheckContrast("rail label", RequireText(railSlot, "Label"), frameImage, 4.5f, contrastFailures);
            CheckContrast("presentation", RequireText(guided, ContentRoot + "/Present Pitch/Presentation"),
                frameImage, 4.5f, contrastFailures);

            var results = RequireChild(canvas, "Results");
            var resultsFrame = RequireChild(results, "Content Frame").GetComponent<Image>();
            CheckContrast("results heading", RequireText(results, "Content Frame/Heading"),
                resultsFrame, 4.5f, contrastFailures);
            var partCard = RequireChild(results, "Content Frame/Results Scroll/Viewport/Content/Problem Card");
            CheckContrast("result sentence", RequireText(partCard, "Sentence"),
                partCard.GetComponent<Image>(), 4.5f, contrastFailures);
            CheckContrast("result status", RequireText(partCard, "Header/Status"),
                partCard.GetComponent<Image>(), 4.5f, contrastFailures);
            var submit = RequireChild(results, "Content Frame/Footer/Submit Button");
            CheckContrast("submit label", RequireText(submit, "Label"),
                submit.GetComponent<Image>(), 4.5f, contrastFailures);

            var focusOutline = sentenceCard.Find("Focus Outline").GetComponent<Image>();
            Assert.That(focusOutline.color, Is.EqualTo(FocusGold));
            var focusChange = ContrastRatio(focusOutline.color, frameImage.color);
            if (focusChange < 3f)
            {
                contrastFailures.Add($"Focus outline change contrast was {focusChange:F2}:1; expected 3:1.");
            }
            var revisionOutline = boardSlot.Find("Revision Outline").GetComponent<Image>();
            var revisionContrast = ContrastRatio(revisionOutline.color, frameImage.color);
            if (revisionContrast < 3f)
            {
                contrastFailures.Add($"Revision outline contrast was {revisionContrast:F2}:1; expected 3:1.");
            }
            Assert.That(contrastFailures, Is.Empty, string.Join(Environment.NewLine, contrastFailures));

            var environmentImage = RequireChild(guided, "Environment Frame").GetComponent<Image>();
            Assert.That(environmentImage.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point),
                "The pitch room must stay point filtered.");
            var judgeImage = RequireChild(guided, "Content Frame/Aya Row/Judge Aya").GetComponent<Image>();
            Assert.That(judgeImage.sprite, Is.Not.Null);
            Assert.That(judgeImage.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point),
                "Judge Aya must stay point filtered.");
            Assert.That(judgeImage.preserveAspect, Is.True);
            using (new ActiveScope(guided.gameObject))
            {
                ForceGuidedLayout(canvas, guided);
                var judgeRect = (RectTransform)judgeImage.transform;
                Assert.That(judgeRect.rect.width, Is.EqualTo(judgeImage.sprite.rect.width).Within(0.5f),
                    "Judge Aya must render at her native sprite width without stretching.");
                Assert.That(judgeRect.rect.height, Is.EqualTo(judgeImage.sprite.rect.height).Within(0.5f),
                    "Judge Aya must render at her native sprite height without stretching.");
            }
        }

        [Test]
        public void GeneratedEnvironment_CropsToFillWithoutStretchingInLandscapeAndPortrait()
        {
            foreach (var size in new[] { WideSize, new Vector2(720f, 960f) })
            {
                var canvas = OpenGameCanvas(size);
                var guided = RequireChild(canvas, "Guided Pitch");
                using (new ActiveScope(guided.gameObject))
                {
                    ForceGuidedLayout(canvas, guided);
                    var environment = RequireChild(guided, "Environment Frame");
                    var image = environment.GetComponent<Image>();
                    var fitter = environment.GetComponent<AspectRatioFitter>();
                    Assert.That(fitter, Is.Not.Null,
                        "The original garden must use an aspect-preserving crop-to-fill fitter.");
                    Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));

                    var sourceAspect = image.sprite.rect.width / image.sprite.rect.height;
                    Assert.That(fitter.aspectRatio, Is.EqualTo(sourceAspect).Within(0.0001f));
                    var environmentRect = environment.GetComponent<RectTransform>();
                    var guidedRect = guided.GetComponent<RectTransform>();
                    Assert.That(environmentRect.rect.width / environmentRect.rect.height,
                        Is.EqualTo(sourceAspect).Within(0.001f),
                        size + " must preserve the original garden aspect ratio.");
                    Assert.That(environmentRect.rect.width, Is.GreaterThanOrEqualTo(guidedRect.rect.width - 0.5f));
                    Assert.That(environmentRect.rect.height, Is.GreaterThanOrEqualTo(guidedRect.rect.height - 0.5f));
                    Assert.That(environmentRect.rect.width / image.sprite.rect.width,
                        Is.EqualTo(environmentRect.rect.height / image.sprite.rect.height).Within(0.001f),
                        size + " must scale the garden uniformly on both axes.");
                }
            }
        }

        private sealed class GuidedFixtureData
        {
            public LocalizationCatalog Catalog;
            public string LongestSentence;
            public string LongestQuestion;
            public string LongestHint;
            public string[] LongestFeedbackValues;
            public string ComposedPitch;
            public string LongestEmptyPrompt;

            public string Resolve(string key)
            {
                return Catalog.Resolve("en", key);
            }
        }

        private static GuidedFixtureData LoadGuidedFixture()
        {
            var catalog = LocalizationCatalog.Load(
                File.ReadAllText("Assets/Content/Localization/en.json"));
            var loaded = GuidedPitchContentLoader.Load(
                File.ReadAllText(GuidedContentPath), catalog.GetKeys("en"));
            Assert.That(loaded.IsSuccess, Is.True, "The guided content document must load.");

            var options = loaded.Content.Modes.Values
                .SelectMany(mode => mode.Parts.SelectMany(part => part.Options)
                    .Concat(mode.FollowUp.Options))
                .ToArray();
            string Longest(IEnumerable<string> values)
            {
                return values.OrderByDescending(value => value.Length).First();
            }
            var sentences = options.Select(option => catalog.Resolve("en", option.TextKey)).ToArray();
            var questions = new[]
            {
                catalog.Resolve("en", "guided.improve.instruction"),
                catalog.Resolve("en", "guided.follow_up.question"),
                catalog.Resolve("en", "guided.part.problem.question"),
                catalog.Resolve("en", "guided.part.evidence.question"),
                catalog.Resolve("en", "guided.part.solution.question"),
                catalog.Resolve("en", "guided.part.value.question"),
            };
            var hints = new[]
            {
                catalog.Resolve("en", "guided.part.problem.hint"),
                catalog.Resolve("en", "guided.part.evidence.hint"),
                catalog.Resolve("en", "guided.part.solution.hint"),
                catalog.Resolve("en", "guided.part.value.hint"),
                catalog.Resolve("en", "guided.follow_up.hint"),
            };
            var longestSentences = sentences.OrderByDescending(value => value.Length).Take(4).ToArray();
            return new GuidedFixtureData
            {
                Catalog = catalog,
                LongestSentence = Longest(sentences),
                LongestQuestion = Longest(questions),
                LongestHint = Longest(hints),
                LongestFeedbackValues = new[]
                {
                    Longest(options.Select(option => catalog.Resolve("en", option.Feedback.WorkedKey))),
                    Longest(options.Select(option => catalog.Resolve("en", option.Feedback.MissingKey))),
                    Longest(options.Select(option => catalog.Resolve("en", option.Feedback.ImproveKey))),
                },
                ComposedPitch = string.Join("\n\n", longestSentences),
                LongestEmptyPrompt = Longest(new[]
                {
                    catalog.Resolve("en", "guided.board.add.problem"),
                    catalog.Resolve("en", "guided.board.add.evidence"),
                    catalog.Resolve("en", "guided.board.add.solution"),
                    catalog.Resolve("en", "guided.board.add.value"),
                }),
            };
        }

        private static void AssertGuidedCopyRenders(
            Transform canvas, Vector2 size, GuidedFixtureData fixture, ICollection<string> failures)
        {
            var guided = RequireChild(canvas, "Guided Pitch");
            using (new ActiveScope(guided.gameObject))
            {
                ApplyResponsiveLayout(guided, size);
                var dialogueCard = RequireChild(guided, "Content Frame/Aya Row/Dialogue Card");
                SetActiveText(RequireText(dialogueCard, "Question"), fixture.LongestQuestion);
                SetActiveText(RequireText(dialogueCard, "Hint"), fixture.LongestHint);

                var board = RequireChild(guided, "Content Frame/Pitch Board");
                var boardView = board.GetComponent<PitchBoardView>();
                foreach (var slot in boardView.Slots)
                {
                    SetActiveText(slot.LabelText, fixture.Resolve(
                        PitchPartVisuals.Get(slot.Part).LabelKey));
                    SetActiveText(slot.SentenceText, fixture.LongestSentence);
                    slot.EmptyPromptText.gameObject.SetActive(false);
                }
                var rail = RequireChild(guided, "Content Frame/Progress Rail");
                var railView = rail.GetComponent<PitchProgressRailView>();
                foreach (var slot in railView.Slots)
                {
                    SetActiveText(slot.LabelText, fixture.Resolve(
                        PitchPartVisuals.Get(slot.Part).LabelKey));
                }

                SetPhaseSections(guided, "Sentence Cards");
                var cards = RequireChild(guided, ContentRoot + "/Sentence Cards");
                var cardLabels = cards.GetComponent<SentenceCardListView>().Cards
                    .Select(card => RequireText(card.transform, "Label")).ToArray();
                foreach (var label in cardLabels)
                {
                    SetActiveText(label, fixture.LongestSentence);
                }
                ForceGuidedLayout(canvas, guided);
                AssertFullyRendered(RequireText(dialogueCard, "Question"), "question", size, failures);
                AssertFullyRendered(RequireText(dialogueCard, "Hint"), "hint", size, failures);
                foreach (var slot in boardView.Slots)
                {
                    AssertFullyRendered(slot.LabelText, "board label " + slot.Part, size, failures);
                    AssertFullyRendered(slot.SentenceText, "board sentence " + slot.Part, size, failures);
                }
                foreach (var slot in railView.Slots)
                {
                    AssertFullyRendered(slot.LabelText, "rail label " + slot.Part, size, failures);
                }
                foreach (var label in cardLabels)
                {
                    AssertFullyRendered(label, "sentence card", size, failures);
                }

                foreach (var slot in boardView.Slots)
                {
                    slot.SentenceText.gameObject.SetActive(false);
                    SetActiveText(slot.EmptyPromptText, fixture.LongestEmptyPrompt);
                }
                ForceGuidedLayout(canvas, guided);
                foreach (var slot in boardView.Slots)
                {
                    AssertFullyRendered(slot.EmptyPromptText, "board prompt " + slot.Part, size, failures);
                }

                SetPhaseSections(guided, "Feedback");
                var feedback = RequireChild(guided, ContentRoot + "/Feedback");
                var rows = feedback.GetComponent<PitchFeedbackView>().Rows;
                var rowKeys = new[]
                {
                    "guided.feedback.worked", "guided.feedback.missing", "guided.feedback.improve",
                };
                for (var index = 0; index < rows.Count; index++)
                {
                    rows[index].Root.SetActive(true);
                    SetActiveText(rows[index].LabelText, fixture.Resolve(rowKeys[index]));
                    SetActiveText(rows[index].ValueText, fixture.LongestFeedbackValues[index]);
                }
                ForceGuidedLayout(canvas, guided);
                for (var index = 0; index < rows.Count; index++)
                {
                    AssertFullyRendered(rows[index].LabelText, "feedback label " + index, size, failures);
                    AssertFullyRendered(rows[index].ValueText, "feedback value " + index, size, failures);
                }

                SetPhaseSections(guided, "Present Pitch");
                var presentation = RequireText(guided, ContentRoot + "/Present Pitch/Presentation");
                SetActiveText(presentation, fixture.ComposedPitch);
                ForceGuidedLayout(canvas, guided);
                AssertFullyRendered(presentation, "presentation", size, failures);

                SetPhaseSections(guided, "Learn");
                var learn = RequireChild(guided, ContentRoot + "/Learn");
                var learnView = learn.GetComponent<LearnPitchView>();
                SetActiveText(learnView.IncompletePitchText,
                    fixture.Resolve("guided.learn.incomplete_pitch"));
                SetActiveText(learnView.ExplanationText, fixture.Resolve("guided.learn.explanation"));
                ForceGuidedLayout(canvas, guided);
                AssertFullyRendered(learnView.IncompletePitchText, "learn pitch", size, failures);
                AssertFullyRendered(learnView.ExplanationText, "learn explanation", size, failures);

                SetPhaseSections(guided, "Mode Selection");
                var modeSelection = RequireChild(guided, ContentRoot + "/Mode Selection");
                var modeCards = modeSelection.GetComponent<ModeSelectionView>().Cards;
                SetActiveText(modeCards[0].TitleText, fixture.Resolve("guided.mode.primary.title"));
                SetActiveText(modeCards[0].DescriptionText, fixture.Resolve("guided.mode.primary.parts"));
                SetActiveText(modeCards[1].TitleText, fixture.Resolve("guided.mode.secondary.title"));
                SetActiveText(modeCards[1].DescriptionText, fixture.Resolve("guided.mode.secondary.parts"));
                ForceGuidedLayout(canvas, guided);
                foreach (var card in modeCards)
                {
                    AssertFullyRendered(card.TitleText, "mode title " + card.Mode, size, failures);
                    AssertFullyRendered(card.DescriptionText, "mode parts " + card.Mode, size, failures);
                }

                SetPhaseSections(guided, "Improve Actions");
                var improve = RequireChild(guided, ContentRoot + "/Improve Actions");
                var strengthenLabels = new List<Text>();
                foreach (Transform button in improve)
                {
                    var label = RequireText(button, "Label");
                    SetActiveText(label, fixture.Resolve("guided.mastery.needs_practice"));
                    strengthenLabels.Add(label);
                }
                ForceGuidedLayout(canvas, guided);
                foreach (var label in strengthenLabels)
                {
                    AssertFullyRendered(label, "strengthen label", size, failures);
                }
            }
        }

        private static void AssertResultsCopyRenders(
            Transform canvas, Vector2 size, GuidedFixtureData fixture, ICollection<string> failures)
        {
            var results = RequireChild(canvas, "Results");
            using (new ActiveScope(results.gameObject))
            {
                SetActiveText(RequireText(results, "Content Frame/Heading"), fixture.Resolve("ui.results"));
                var partViews = results.GetComponentsInChildren<PitchResultPartView>(true);
                foreach (var view in partViews)
                {
                    view.gameObject.SetActive(true);
                    SetActiveText(view.LabelText, fixture.Resolve(
                        PitchPartVisuals.Get(view.Part).LabelKey));
                    SetActiveText(view.SentenceText, fixture.LongestSentence);
                    SetActiveText(view.StatusText, fixture.Resolve("guided.mastery.needs_practice"));
                    SetActiveText(view.RevisionNoteText, fixture.Resolve("guided.results.part.revised"));
                }
                var content = RequireChild(results, "Content Frame/Results Scroll/Viewport/Content");
                SetActiveText(RequireText(content, "Readiness"),
                    fixture.Resolve("guided.pitch_readiness") + ": 100%");
                SetActiveText(RequireText(content, "Improvement"),
                    fixture.Resolve("guided.results.strengthened.many").Replace("{count}", "4"));
                SetActiveText(RequireText(content, "Final Pitch Heading"),
                    fixture.Resolve("guided.results.final_pitch"));
                SetActiveText(RequireText(content, "Final Pitch"), fixture.ComposedPitch);
                SetActiveText(RequireText(content, "Transfer Prompt"),
                    fixture.Resolve("guided.transfer_prompt"));
                SetActiveText(RequireText(results, "Content Frame/Submission Status"),
                    fixture.Resolve("lms.submission.success"));
                SetActiveText(RequireText(results, "Content Frame/Footer/Submit Button/Label"),
                    fixture.Resolve("guided.results.resubmit"));
                SetActiveText(RequireText(results, "Content Frame/Footer/Retry Button/Label"),
                    fixture.Resolve("ui.retry"));
                ForceResultsLayout(canvas, results);

                AssertFullyRendered(RequireText(results, "Content Frame/Heading"),
                    "results heading", size, failures);
                foreach (var view in partViews)
                {
                    AssertFullyRendered(view.LabelText, "result label " + view.Part, size, failures);
                    AssertFullyRendered(view.SentenceText, "result sentence " + view.Part, size, failures);
                    AssertFullyRendered(view.StatusText, "result status " + view.Part, size, failures);
                    AssertFullyRendered(view.RevisionNoteText, "result note " + view.Part, size, failures);
                }
                AssertFullyRendered(RequireText(content, "Readiness"), "readiness", size, failures);
                AssertFullyRendered(RequireText(content, "Improvement"), "improvement", size, failures);
                AssertFullyRendered(RequireText(content, "Final Pitch"), "final pitch", size, failures);
                AssertFullyRendered(RequireText(content, "Transfer Prompt"), "transfer", size, failures);
                AssertFullyRendered(RequireText(results, "Content Frame/Submission Status"),
                    "submission status", size, failures);
                AssertFullyRendered(RequireText(results, "Content Frame/Footer/Submit Button/Label"),
                    "submit label", size, failures);
                AssertFullyRendered(RequireText(results, "Content Frame/Footer/Retry Button/Label"),
                    "retry label", size, failures);
            }
        }

        private static void AssertSupportScreenCopyRenders(
            Transform canvas, Vector2 size, GuidedFixtureData fixture, ICollection<string> failures)
        {
            var briefing = RequireChild(canvas, "Briefing");
            using (new ActiveScope(briefing.gameObject))
            {
                var briefingKeys = new[]
                {
                    "guided.title", "guided.briefing", "guided.briefing.judge",
                    "guided.briefing.practice", "guided.briefing.untimed",
                };
                var lines = new List<Text>();
                for (var index = 0; index < briefingKeys.Length; index++)
                {
                    var line = RequireText(briefing, $"Content Frame/Line {index + 1}");
                    SetActiveText(line, fixture.Resolve(briefingKeys[index]));
                    lines.Add(line);
                }
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(briefing.GetComponent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    RequireChild(briefing, "Content Frame").GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                foreach (var line in lines)
                {
                    AssertFullyRendered(line, "briefing " + line.name, size, failures);
                }
            }

            var fallback = RequireChild(canvas, "Safe Fallback");
            using (new ActiveScope(fallback.gameObject))
            {
                var message = RequireText(fallback, "Content Frame/Recovery Message");
                SetActiveText(message, SafeFallbackPresenter.EnglishRecoveryMessage);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(fallback.GetComponent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    RequireChild(fallback, "Content Frame").GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                AssertFullyRendered(message, "recovery message", size, failures);
            }
        }

        private static void AssertGuidedGameContract(Scene scene)
        {
            Assert.That(scene.GetRootGameObjects().Count(root => root.name == "Generated UI"), Is.EqualTo(1));
            Assert.That(scene.GetRootGameObjects().Count(root => root.name == SentinelName), Is.EqualTo(1));
            Assert.That(FindInScene<Canvas>(scene), Has.Length.EqualTo(1));
            Assert.That(FindInScene<EventSystem>(scene), Has.Length.EqualTo(1));
            Assert.That(FindInScene<GuidedPitchScreenRouter>(scene), Has.Length.EqualTo(1));

            var generated = scene.GetRootGameObjects().Single(root => root.name == "Generated UI");
            var canvas = generated.transform.Find("Canvas");
            Assert.That(canvas, Is.Not.Null);
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));

            Assert.That(canvas.Cast<Transform>().Select(child => child.name), Is.EqualTo(new[]
            {
                "Title", "Briefing", "Guided Pitch", "Results", "Settings", "Safe Fallback",
            }));

            var guided = canvas.Find("Guided Pitch");
            Assert.That(guided.Cast<Transform>().Select(child => child.name),
                Is.EqualTo(new[] { "Environment Frame", "Content Frame" }));
            var frame = guided.Find("Content Frame");
            Assert.That(frame.Cast<Transform>().Select(child => child.name), Is.EqualTo(new[]
            {
                "Progress Rail", "Aya Row", "Pitch Board", "Phase Scroll", "Primary Action",
            }));
            Assert.That(frame.Find("Aya Row").Cast<Transform>().Select(child => child.name),
                Is.EqualTo(new[] { "Judge Aya", "Dialogue Card" }));
            var content = frame.Find("Phase Scroll/Viewport/Content");
            Assert.That(content, Is.Not.Null,
                "Phase Scroll must own a Viewport/Content scroll hierarchy.");
            Assert.That(content.Cast<Transform>().Select(child => child.name),
                Is.EqualTo(PhaseSectionNames));

            var router = canvas.GetComponent<GuidedPitchScreenRouter>();
            Assert.That(router, Is.Not.Null);
            Assert.That(router.ValidateContract(out var reason), Is.True, reason);
            var serializedRouter = new SerializedObject(router);
            var modeSelectionPanel = serializedRouter.FindProperty("modeSelectionPanel")
                .objectReferenceValue as GameObject;
            Assert.That(modeSelectionPanel, Is.SameAs(content.Find("Mode Selection").gameObject),
                "The router must route ModeSelection to the nested Phase Scroll section.");

            Assert.That(canvas.Find("Title").gameObject.activeSelf, Is.True);
            foreach (var hidden in new[] { "Briefing", "Guided Pitch", "Results", "Settings", "Safe Fallback" })
            {
                Assert.That(canvas.Find(hidden).gameObject.activeSelf, Is.False,
                    hidden + " must start hidden behind Title.");
            }
            var eventSystem = FindInScene<EventSystem>(scene).Single();
            Assert.That(eventSystem.firstSelectedGameObject,
                Is.SameAs(canvas.Find("Title/Content Frame/Start Button").gameObject));

            AssertNoLegacyPresentation(canvas);
        }

        private static void AssertNoLegacyPresentation(Transform canvas)
        {
            Assert.That(canvas.GetComponentsInChildren<GameScreenRouter>(true), Is.Empty,
                "The legacy router must not remain active in the owned scene.");
            Assert.That(canvas.GetComponentsInChildren<TutorialPresenter>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<PitchRoomPresenter>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<ResultsPresenter>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<ResponseListView>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<TimerView>(true), Is.Empty,
                "The untimed guided flow must not present a timer.");
            Assert.That(canvas.GetComponentsInChildren<ConfidenceView>(true), Is.Empty,
                "The guided flow must not present the confidence meter.");
            Assert.That(canvas.GetComponentsInChildren<QuestionReviewItemView>(true), Is.Empty,
                "The legacy review list must be absent.");
            Assert.That(canvas.GetComponentsInChildren<KeyboardReviewScrollbar>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<JudgeReactionView>(true), Is.Empty,
                "The legacy reaction bridge must not remain in the owned scene.");
            foreach (var legacyName in new[] { "Metrics", "Prompt Backing", "PitchRoom", "Tutorial" })
            {
                Assert.That(canvas.GetComponentsInChildren<Transform>(true)
                        .Count(child => child.name == legacyName), Is.Zero,
                    legacyName + " must be absent from the guided scene.");
            }
        }

        private static void AssertBootstrapContract()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
            try
            {
                var generated = scene.GetRootGameObjects().Single(root => root.name == "Generated Bootstrap");
                var bootstrapper = generated.GetComponentInChildren<Bootstrapper>(true);
                var serialized = new SerializedObject(bootstrapper);
                Assert.That(serialized.FindProperty("scenarioJson"), Is.Null,
                    "The legacy scenario reference must be removed from the Bootstrapper.");
                var guidedJson = serialized.FindProperty("guidedPitchContentJson")
                    .objectReferenceValue as TextAsset;
                Assert.That(guidedJson, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(guidedJson), Is.EqualTo(GuidedContentPath));

                var sources = bootstrapper.GetComponents<AudioSource>();
                Assert.That(sources, Has.Length.EqualTo(2));
                Assert.That(sources.All(source => !source.playOnAwake), Is.True);
                var music = serialized.FindProperty("musicSource").objectReferenceValue as AudioSource;
                var sfx = serialized.FindProperty("sfxSource").objectReferenceValue as AudioSource;
                Assert.That(music, Is.Not.Null.And.Not.SameAs(sfx));
                Assert.That(music.loop, Is.True);
                Assert.That(sfx.loop, Is.False);
                var bindings = serialized.FindProperty("audioCueBindings");
                Assert.That(bindings.arraySize, Is.EqualTo(Enum.GetValues(typeof(AudioCue)).Length));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertWebIntegrationContract()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/WebIntegrationTest.unity", OpenSceneMode.Additive);
            try
            {
                var ownedRoots = scene.GetRootGameObjects()
                    .Where(root => root.name == "Generated Web Integration Test")
                    .ToArray();
                Assert.That(ownedRoots, Has.Length.EqualTo(1));
                var hosts = ownedRoots[0].GetComponentsInChildren<WebGlLmsBridgeHost>(true);
                Assert.That(hosts, Has.Length.EqualTo(1));
                var serialized = new SerializedObject(hosts[0]);
                var diagnostics = serialized.FindProperty("diagnosticsLabel").objectReferenceValue as Text;
                Assert.That(diagnostics, Is.Not.Null);
                var missingScripts = scene.GetRootGameObjects()
                    .SelectMany(sceneRoot => sceneRoot.GetComponentsInChildren<Transform>(true))
                    .Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
                Assert.That(missingScripts, Is.Zero);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertExactBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(2));
            Assert.That(scenes.All(scene => scene.enabled), Is.True);
            Assert.That(scenes.Select(scene => scene.path), Is.EqualTo(new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Game.unity",
            }));
        }

        private static Transform OpenGameCanvas(Vector2 size)
        {
            var scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
            var generated = scene.GetRootGameObjects().Single(root => root.name == "Generated UI");
            var canvas = generated.transform.Find("Canvas");
            Assert.That(canvas, Is.Not.Null, "The generated scene must own a Canvas.");
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = size;
            return canvas;
        }

        private static void ApplyResponsiveLayout(Transform guided, Vector2 size)
        {
            var layout = guided.GetComponent<GuidedPitchResponsiveLayout>();
            Assert.That(layout, Is.Not.Null, "Guided Pitch must carry GuidedPitchResponsiveLayout.");
            layout.Apply(size);
        }

        private static void SetPhaseSections(Transform guided, params string[] activeSections)
        {
            var content = RequireChild(guided, ContentRoot);
            foreach (var sectionName in PhaseSectionNames)
            {
                var section = content.Find(sectionName);
                Assert.That(section, Is.Not.Null, "Missing phase section " + sectionName);
                section.gameObject.SetActive(Array.IndexOf(activeSections, sectionName) >= 0);
            }
        }

        private static void ForceGuidedLayout(Transform canvas, Transform guided)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(guided.GetComponent<RectTransform>());
            var frame = guided.Find("Content Frame");
            if (frame != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(frame.GetComponent<RectTransform>());
                var scrollContent = frame.Find("Phase Scroll/Viewport/Content");
                if (scrollContent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent.GetComponent<RectTransform>());
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(frame.GetComponent<RectTransform>());
            }
            Canvas.ForceUpdateCanvases();
        }

        private static void ForceResultsLayout(Transform canvas, Transform results)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(results.GetComponent<RectTransform>());
            var frame = results.Find("Content Frame");
            if (frame != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(frame.GetComponent<RectTransform>());
                var scrollContent = frame.Find("Results Scroll/Viewport/Content");
                if (scrollContent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent.GetComponent<RectTransform>());
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(frame.GetComponent<RectTransform>());
            }
            Canvas.ForceUpdateCanvases();
        }

        private static Transform RequireChild(Transform parent, string path)
        {
            var child = parent.Find(path);
            Assert.That(child, Is.Not.Null, $"Missing '{path}' under '{parent.name}'.");
            return child;
        }

        private static Text RequireText(Transform parent, string path)
        {
            var text = RequireChild(parent, path).GetComponent<Text>();
            Assert.That(text, Is.Not.Null, $"'{path}' under '{parent.name}' must carry a Text.");
            return text;
        }

        private static void SetActiveText(Text text, string value)
        {
            Assert.That(text, Is.Not.Null);
            text.gameObject.SetActive(true);
            text.text = value;
        }

        private static void AssertFullyRendered(
            Text text, string label, Vector2 size, ICollection<string> failures)
        {
            var value = text.text;
            var generator = text.cachedTextGenerator;
            generator.Populate(value, text.GetGenerationSettings(text.rectTransform.rect.size));
            var expectedVisible = value.Count(character => character != '\n');
            if (generator.characterCountVisible < expectedVisible)
            {
                failures.Add(
                    $"{label} at {size}: rendered {generator.characterCountVisible}/{expectedVisible} " +
                    $"characters in {text.rectTransform.rect.width:F1}x{text.rectTransform.rect.height:F1}px.");
            }
        }

        private static void CheckContrast(
            string label, Text text, Image backing, float minimum, ICollection<string> failures)
        {
            if (backing == null)
            {
                failures.Add(label + " has no opaque backing.");
                return;
            }
            if (backing.color.a < 1f)
            {
                failures.Add(label + " backing must be opaque.");
            }
            var ratio = ContrastRatio(text.color, backing.color);
            if (ratio < minimum)
            {
                failures.Add($"{label} contrast was {ratio:F2}:1; expected {minimum:F1}:1.");
            }
        }

        private static float ContrastRatio(Color first, Color second)
        {
            var firstLuminance = RelativeLuminance(first);
            var secondLuminance = RelativeLuminance(second);
            return (Mathf.Max(firstLuminance, secondLuminance) + 0.05f) /
                (Mathf.Min(firstLuminance, secondLuminance) + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= 0.04045f
                ? value / 12.92f
                : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static void AssertContained(RectTransform parent, RectTransform child)
        {
            Assert.That(child, Is.Not.Null);
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var local = parent.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(parent.rect.xMin - 0.5f, parent.rect.xMax + 0.5f),
                    child.name + " escaped " + parent.name + " horizontally.");
                Assert.That(local.y, Is.InRange(parent.rect.yMin - 0.5f, parent.rect.yMax + 0.5f),
                    child.name + " escaped " + parent.name + " vertically.");
            }
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }

        private sealed class ActiveScope : IDisposable
        {
            private readonly GameObject target;
            private readonly bool wasActive;

            public ActiveScope(GameObject target)
            {
                this.target = target;
                wasActive = target.activeSelf;
                target.SetActive(true);
            }

            public void Dispose()
            {
                target.SetActive(wasActive);
            }
        }
    }
}
