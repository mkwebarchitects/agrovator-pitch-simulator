using System.IO;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.GuidedPitch;
using Agrovator.PitchSimulator.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    /// <summary>
    /// The sentence cards sit in a GridLayoutGroup with a fixed row/column count
    /// of one, set by GuidedPitchResponsiveLayout so three cards lay out as one
    /// row (wide) or one column (compact). A fixed constraint count always
    /// reserves that one row/column's cell height regardless of how many
    /// children are active, unlike PitchFeedbackView's plain VerticalLayoutGroup
    /// rows, which shrink to nothing once cleared. These tests pin that the
    /// sentence card container matches the feedback rows instead.
    /// </summary>
    public sealed class SentenceCardListViewLayoutTests
    {
        private const string GuidedContentPath = "Assets/Content/Scenarios/guided-pitch-builder.en.json";

        private GameObject canvasRoot;

        [TearDown]
        public void TearDown()
        {
            if (canvasRoot != null)
            {
                Object.DestroyImmediate(canvasRoot);
            }
        }

        [Test]
        public void SentenceCards_CollapseToZeroHeightWhenCleared_LikeTheFeedbackRowsDo()
        {
            var options = LoadThreeAuthoredOptions();
            var (list, contentRect) = BuildRig();

            list.Render(options, null, true, key => key);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Assert.That(contentRect.rect.height, Is.GreaterThanOrEqualTo(96f),
                "Three populated sentence cards must reserve their full cell height.");

            list.Clear();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Assert.That(contentRect.rect.height, Is.LessThan(1f),
                "Cleared sentence cards must collapse to (near) zero height, the same way the feedback " +
                "rows collapse, instead of permanently reserving a fixed row of empty space.");
        }

        [Test]
        public void SentenceCards_RenderedWithNoOptions_AlsoCollapseToZeroHeight()
        {
            var (list, contentRect) = BuildRig();

            list.Render(System.Array.Empty<GuidedPitchOption>(), null, true, key => key);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Assert.That(contentRect.rect.height, Is.LessThan(1f),
                "Rendering zero options must collapse the container exactly like Clear() does.");
        }

        private (SentenceCardListView list, RectTransform contentRect) BuildRig()
        {
            canvasRoot = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var contentRoot = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentRoot.transform.SetParent(canvasRoot.transform, false);
            var contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
            contentRoot.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var cardsRoot = new GameObject("Sentence Cards", typeof(RectTransform),
                typeof(GridLayoutGroup), typeof(SentenceCardListView));
            cardsRoot.transform.SetParent(contentRoot.transform, false);
            var grid = cardsRoot.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            grid.cellSize = new Vector2(288f, 96f);

            var cards = Enumerable.Range(0, 3)
                .Select(index => BuildCard("Card " + index, cardsRoot.transform))
                .ToArray();
            var list = cardsRoot.GetComponent<SentenceCardListView>();
            list.Configure(cards);
            list.Initialize(_ => { });

            return (list, (RectTransform)contentRoot.transform);
        }

        private static GuidedPitchOption[] LoadThreeAuthoredOptions()
        {
            var catalog = LocalizationCatalog.Load(
                File.ReadAllText("Assets/Content/Localization/en.json"));
            var loaded = GuidedPitchContentLoader.Load(
                File.ReadAllText(GuidedContentPath), catalog.GetKeys("en"));
            Assert.That(loaded.IsSuccess, Is.True, "The guided content document must load.");

            return loaded.Content.Modes.Values.First().Parts.First().Options.ToArray();
        }

        private static SentenceCardView BuildCard(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button), typeof(SentenceCardView));
            root.transform.SetParent(parent, false);
            var background = root.GetComponent<Image>();
            var button = root.GetComponent<Button>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.GetComponent<Text>();

            var focusObject = new GameObject("Focus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            focusObject.transform.SetParent(root.transform, false);
            var focus = focusObject.GetComponent<Image>();

            var card = root.GetComponent<SentenceCardView>();
            card.Configure(button, label, background, focus);
            return card;
        }
    }
}
