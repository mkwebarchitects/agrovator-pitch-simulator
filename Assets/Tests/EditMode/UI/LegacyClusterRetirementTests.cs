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
        /// <summary>
        /// Matched on full name, not bare name: a future
        /// <c>GuidedPitch.GameState</c> is a different type with every right
        /// to exist, and failing it with a message about a retirement it has
        /// nothing to do with would be a false alarm aimed at the wrong code.
        /// </summary>
        private static readonly string[] RetiredTypeFullNames =
        {
            "Agrovator.PitchSimulator.UI.GameScreenRouter",
            "Agrovator.PitchSimulator.UI.PitchRoomPresenter",
            "Agrovator.PitchSimulator.UI.TutorialPresenter",
            "Agrovator.PitchSimulator.UI.ResultsPresenter",
            "Agrovator.PitchSimulator.UI.ConfidenceView",
            "Agrovator.PitchSimulator.UI.TimerView",
            "Agrovator.PitchSimulator.UI.ResponseListView",
            "Agrovator.PitchSimulator.UI.ResponseButtonView",
            "Agrovator.PitchSimulator.UI.QuestionReviewItemView",
            "Agrovator.PitchSimulator.UI.KeyboardReviewScrollbar",
            "Agrovator.PitchSimulator.UI.FocusNavigator",
            "Agrovator.PitchSimulator.Core.PitchSessionController",
            "Agrovator.PitchSimulator.Core.PitchSessionSnapshot",
            "Agrovator.PitchSimulator.Core.PitchSessionEvent",
            "Agrovator.PitchSimulator.Core.PitchSessionEventType",
            "Agrovator.PitchSimulator.Core.GameState",
            "Agrovator.PitchSimulator.Core.GameStateMachine",
            "Agrovator.PitchSimulator.Core.GameCommand",
            "Agrovator.PitchSimulator.Core.QuestionTimer",
            "Agrovator.PitchSimulator.Dialogue.DialogueSession",
            "Agrovator.PitchSimulator.Dialogue.RuntimeScenario",
            "Agrovator.PitchSimulator.Dialogue.ScenarioJsonLoader",
            "Agrovator.PitchSimulator.Dialogue.ScenarioValidator",
            "Agrovator.PitchSimulator.Dialogue.Unity.ScenarioAsset",
            "Agrovator.PitchSimulator.Dialogue.ResponseAvailability",
            "Agrovator.PitchSimulator.Dialogue.ValidationIssue",
            "Agrovator.PitchSimulator.Scoring.ConfidenceMeter",
            "Agrovator.PitchSimulator.Scoring.ResultBuilder",
            "Agrovator.PitchSimulator.Scoring.ResultLevel",
            "Agrovator.PitchSimulator.Scoring.ScoreAccumulator",
            "Agrovator.PitchSimulator.Scoring.ScoreCategory",
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
                .SelectMany(LoadableTypes)
                .Select(type => type.FullName)
                .Where(name => RetiredTypeFullNames.Contains(name, StringComparer.Ordinal))
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

        /// <summary>
        /// A partially loadable assembly throws rather than returning what it
        /// has, which would turn this guard into an unrelated crash. The types
        /// it did load are still worth checking.
        /// </summary>
        private static System.Type[] LoadableTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
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
