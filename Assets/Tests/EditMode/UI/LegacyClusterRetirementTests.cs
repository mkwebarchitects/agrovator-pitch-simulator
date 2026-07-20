using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.UI
{
    /// <summary>
    /// The legacy question-and-answer presentation and session stack is
    /// unreachable from <c>Bootstrapper</c>, which composes only the guided
    /// pitch session. Scene-level guards prove it is absent from the generated
    /// scenes; this proves it is absent from the project at all, so it cannot
    /// be re-wired, cannot be re-tested into life, and cannot be compiled into
    /// the WebGL player a learner downloads.
    /// </summary>
    public sealed class LegacyClusterRetirementTests
    {
        private static readonly string[] RetiredTypeNames =
        {
            "GameScreenRouter", "PitchRoomPresenter", "TutorialPresenter", "ResultsPresenter",
            "ConfidenceView", "TimerView", "ResponseListView", "ResponseButtonView",
            "QuestionReviewItemView", "KeyboardReviewScrollbar", "FocusNavigator",
            "PitchSessionController", "PitchSessionSnapshot", "PitchSessionEvent",
            "PitchSessionEventType", "GameState", "GameStateMachine", "GameCommand",
            "QuestionTimer", "DialogueSession", "RuntimeScenario", "ScenarioJsonLoader",
            "ScenarioValidator", "ScenarioAsset", "ResponseAvailability", "ValidationIssue",
            "ConfidenceMeter", "ResultBuilder", "ResultLevel", "ScoreAccumulator",
            "ScoreCategory",
        };

        private static readonly string[] RetiredAssemblyNames =
        {
            "Agrovator.PitchSimulator.Dialogue",
            "Agrovator.PitchSimulator.Dialogue.Unity",
            "Agrovator.PitchSimulator.Scoring",
        };

        private const string RetiredScenarioPath =
            "Assets/Content/Scenarios/smart-school-garden.en.json";

        [Test]
        public void RetiredLegacyTypes_AreAbsentFromEveryProjectAssembly()
        {
            var survivors = ProjectAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Select(type => type.Name)
                .Where(name => RetiredTypeNames.Contains(name, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(survivors, Is.Empty,
                "The legacy presentation and session cluster is unreachable from Bootstrapper " +
                "and must not remain compiled into the project: " + string.Join(", ", survivors));
        }

        [Test]
        public void RetiredLegacyAssemblies_AreNotLoaded()
        {
            var survivors = ProjectAssemblies()
                .Select(assembly => assembly.GetName().Name)
                .Where(name => RetiredAssemblyNames.Contains(name, StringComparer.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(survivors, Is.Empty,
                "Assemblies consumed only by the retired session controller must be removed " +
                "so no assembly boundary outlives its only consumer: " + string.Join(", ", survivors));
        }

        [Test]
        public void RetiredLegacyScenarioContent_IsAbsent()
        {
            Assert.That(File.Exists(RetiredScenarioPath), Is.False,
                RetiredScenarioPath + " belongs to the retired dialogue scenario format and " +
                "is loaded by nothing the learner can reach.");
            Assert.That(File.Exists(RetiredScenarioPath + ".meta"), Is.False,
                "A retired asset must not leave its .meta file behind.");
        }

        private static System.Reflection.Assembly[] ProjectAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name
                    .StartsWith("Agrovator.PitchSimulator", StringComparison.Ordinal))
                .ToArray();
        }
    }
}
