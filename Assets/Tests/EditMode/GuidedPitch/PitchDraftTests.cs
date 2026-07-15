using System;
using System.Collections.Generic;
using System.Linq;
using Agrovator.PitchSimulator.GuidedPitch;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.GuidedPitch
{
    public sealed class PitchDraftTests
    {
        [Test]
        public void PitchParts_UsesTheAuthoredPresentationOrder()
        {
            Assert.That(PitchParts.Ordered, Is.EqualTo(new[]
            {
                PitchPart.Problem,
                PitchPart.Evidence,
                PitchPart.Solution,
                PitchPart.Value,
            }));
        }

        [Test]
        public void NewDraft_IsEmptyAndCannotReviseAnEmptyPart()
        {
            var draft = new PitchDraft();

            Assert.That(draft.Snapshot.IsComplete, Is.False);
            Assert.That(draft.Snapshot.PopulatedCount, Is.Zero);
            Assert.That(
                draft.TryRevise(PitchPart.Problem, "replacement", MasteryState.Clear),
                Is.False);
        }

        [Test]
        public void InitialSelection_IsAcceptedOnlyOnceAndPreservedByRevision()
        {
            var draft = new PitchDraft();

            Assert.That(
                draft.TrySelectInitial(
                    PitchPart.Problem,
                    "primary-problem-clear",
                    MasteryState.Clear),
                Is.True);
            Assert.That(
                draft.TrySelectInitial(PitchPart.Problem, "duplicate", MasteryState.Developing),
                Is.False);
            Assert.That(
                draft.Snapshot[PitchPart.Problem].InitialResponseId,
                Is.EqualTo("primary-problem-clear"));
            Assert.That(
                draft.Snapshot[PitchPart.Problem].CurrentResponseId,
                Is.EqualTo("primary-problem-clear"));
            Assert.That(draft.Snapshot[PitchPart.Problem].WasRevised, Is.False);

            Assert.That(
                draft.TryRevise(
                    PitchPart.Problem,
                    "primary-problem-developing",
                    MasteryState.Developing),
                Is.True);
            Assert.That(
                draft.Snapshot[PitchPart.Problem].InitialResponseId,
                Is.EqualTo("primary-problem-clear"));
            Assert.That(
                draft.Snapshot[PitchPart.Problem].CurrentResponseId,
                Is.EqualTo("primary-problem-developing"));
            Assert.That(draft.Snapshot[PitchPart.Problem].WasRevised, Is.True);
        }

        [Test]
        public void CompleteDraft_CalculatesReadinessCompetenciesAndMeans()
        {
            var draft = CreateCompleteDraft(
                MasteryState.Clear,
                MasteryState.Developing,
                MasteryState.NeedsPractice,
                MasteryState.Clear);

            var snapshot = draft.Snapshot;
            var assessment = PitchAssessmentBuilder.Build(snapshot);

            Assert.That(snapshot.IsComplete, Is.True);
            Assert.That(snapshot.PopulatedCount, Is.EqualTo(4));
            Assert.That(assessment.PitchReadiness, Is.EqualTo(80));
            Assert.That(assessment.ProblemClarity, Is.EqualTo(100));
            Assert.That(assessment.EvidenceQuality, Is.EqualTo(70));
            Assert.That(assessment.SolutionFit, Is.EqualTo(40));
            Assert.That(assessment.AudienceValue, Is.EqualTo(100));
            Assert.That(assessment.ClearExplanation, Is.EqualTo(78));
            Assert.That(assessment.Communication, Is.EqualTo(78));
            Assert.That(assessment.ImprovedPartCount, Is.Zero);
            Assert.That(assessment.MasteryByPart, Is.EqualTo(new Dictionary<PitchPart, MasteryState>
            {
                { PitchPart.Problem, MasteryState.Clear },
                { PitchPart.Evidence, MasteryState.Developing },
                { PitchPart.Solution, MasteryState.NeedsPractice },
                { PitchPart.Value, MasteryState.Clear },
            }));
        }

        [Test]
        public void EmptySlots_ContributeZeroAndMeansUseOnlyPopulatedParts()
        {
            var draft = new PitchDraft();
            draft.TrySelectInitial(PitchPart.Problem, "problem", MasteryState.NeedsPractice);
            draft.TrySelectInitial(PitchPart.Evidence, "evidence", MasteryState.Developing);

            var assessment = PitchAssessmentBuilder.Build(draft.Snapshot);

            Assert.That(assessment.PitchReadiness, Is.EqualTo(30));
            Assert.That(assessment.ProblemClarity, Is.EqualTo(40));
            Assert.That(assessment.EvidenceQuality, Is.EqualTo(70));
            Assert.That(assessment.SolutionFit, Is.Zero);
            Assert.That(assessment.AudienceValue, Is.Zero);
            Assert.That(assessment.ClearExplanation, Is.EqualTo(55));
            Assert.That(assessment.Communication, Is.EqualTo(55));
            Assert.That(assessment.MasteryByPart.Keys, Is.EqualTo(new[]
            {
                PitchPart.Problem,
                PitchPart.Evidence,
            }));
        }

        [Test]
        public void EmptyDraft_AssessesToZero()
        {
            var assessment = PitchAssessmentBuilder.Build(new PitchDraft().Snapshot);

            Assert.That(assessment.PitchReadiness, Is.Zero);
            Assert.That(assessment.ProblemClarity, Is.Zero);
            Assert.That(assessment.EvidenceQuality, Is.Zero);
            Assert.That(assessment.SolutionFit, Is.Zero);
            Assert.That(assessment.AudienceValue, Is.Zero);
            Assert.That(assessment.ClearExplanation, Is.Zero);
            Assert.That(assessment.Communication, Is.Zero);
            Assert.That(assessment.ImprovedPartCount, Is.Zero);
            Assert.That(assessment.MasteryByPart, Is.Empty);
        }

        [Test]
        public void ImprovedPartCount_CountsOnlyMasteryRankIncreasesFromInitialSelection()
        {
            var draft = CreateCompleteDraft(
                MasteryState.NeedsPractice,
                MasteryState.Clear,
                MasteryState.Developing,
                MasteryState.Clear);

            draft.TryRevise(PitchPart.Problem, "problem-improved", MasteryState.Developing);
            draft.TryRevise(PitchPart.Evidence, "evidence-declined", MasteryState.Developing);
            draft.TryRevise(PitchPart.Solution, "solution-same", MasteryState.Developing);
            draft.TryRevise(PitchPart.Value, "value-reworded", MasteryState.Clear);

            var assessment = PitchAssessmentBuilder.Build(draft.Snapshot);

            Assert.That(assessment.ImprovedPartCount, Is.EqualTo(1));
        }

        [Test]
        public void Snapshot_IsAnImmutableCopyOfDraftState()
        {
            var draft = new PitchDraft();
            draft.TrySelectInitial(PitchPart.Problem, "initial", MasteryState.NeedsPractice);
            var beforeRevision = draft.Snapshot;

            draft.TryRevise(PitchPart.Problem, "revised", MasteryState.Clear);
            draft.TrySelectInitial(PitchPart.Evidence, "evidence", MasteryState.Developing);
            var afterRevision = draft.Snapshot;

            Assert.That(beforeRevision.PopulatedCount, Is.EqualTo(1));
            Assert.That(beforeRevision[PitchPart.Problem].CurrentResponseId, Is.EqualTo("initial"));
            Assert.That(beforeRevision[PitchPart.Problem].CurrentMastery, Is.EqualTo(MasteryState.NeedsPractice));
            Assert.That(beforeRevision[PitchPart.Problem].WasRevised, Is.False);
            Assert.That(beforeRevision[PitchPart.Evidence].IsPopulated, Is.False);
            Assert.That(afterRevision[PitchPart.Problem].CurrentResponseId, Is.EqualTo("revised"));
            Assert.That(afterRevision[PitchPart.Evidence].IsPopulated, Is.True);
        }

        [Test]
        public void AssessmentMasteryMap_CannotBeMutatedThroughDictionaryInterfaces()
        {
            var draft = new PitchDraft();
            draft.TrySelectInitial(PitchPart.Problem, "problem", MasteryState.Clear);
            var assessment = PitchAssessmentBuilder.Build(draft.Snapshot);

            var dictionary = assessment.MasteryByPart as IDictionary<PitchPart, MasteryState>;

            Assert.That(dictionary, Is.Not.Null);
            Assert.Throws<NotSupportedException>(
                () => dictionary[PitchPart.Problem] = MasteryState.NeedsPractice);
            Assert.That(
                assessment.MasteryByPart[PitchPart.Problem],
                Is.EqualTo(MasteryState.Clear));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t")]
        public void SelectionAndRevision_RejectBlankResponseIds(string responseId)
        {
            var draft = new PitchDraft();

            Assert.Throws<ArgumentException>(
                () => draft.TrySelectInitial(PitchPart.Problem, responseId, MasteryState.Clear));
            Assert.Throws<ArgumentException>(
                () => draft.TryRevise(PitchPart.Problem, responseId, MasteryState.Clear));
        }

        [Test]
        public void DraftOperations_RejectUnknownPitchParts()
        {
            var draft = new PitchDraft();
            var unknown = (PitchPart)int.MaxValue;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => draft.TrySelectInitial(unknown, "response", MasteryState.Clear));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => draft.TryRevise(unknown, "response", MasteryState.Clear));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = draft.Snapshot[unknown]);
        }

        [Test]
        public void DraftOperations_RejectUnknownMasteryStates()
        {
            var unknown = (MasteryState)int.MaxValue;
            var draft = new PitchDraft();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => draft.TrySelectInitial(PitchPart.Problem, "response", unknown));

            draft.TrySelectInitial(PitchPart.Problem, "response", MasteryState.Clear);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => draft.TryRevise(PitchPart.Problem, "replacement", unknown));
        }

        [Test]
        public void Reset_EmptiesEverySlot()
        {
            var draft = CreateCompleteDraft(
                MasteryState.Clear,
                MasteryState.Developing,
                MasteryState.NeedsPractice,
                MasteryState.Clear);
            draft.TryRevise(PitchPart.Problem, "problem-revised", MasteryState.Developing);

            draft.Reset();

            Assert.That(draft.Snapshot.PopulatedCount, Is.Zero);
            Assert.That(draft.Snapshot.IsComplete, Is.False);
            foreach (var part in PitchParts.Ordered)
            {
                Assert.That(draft.Snapshot[part].Part, Is.EqualTo(part));
                Assert.That(draft.Snapshot[part].InitialResponseId, Is.Null);
                Assert.That(draft.Snapshot[part].CurrentResponseId, Is.Null);
                Assert.That(draft.Snapshot[part].InitialMastery, Is.Null);
                Assert.That(draft.Snapshot[part].CurrentMastery, Is.Null);
                Assert.That(draft.Snapshot[part].IsPopulated, Is.False);
                Assert.That(draft.Snapshot[part].WasRevised, Is.False);
            }
        }

        [Test]
        public void GuidedPitchAssembly_DoesNotReferenceUnityEngine()
        {
            var referencedAssemblyNames = typeof(PitchDraft)
                .Assembly
                .GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.Name);

            Assert.That(referencedAssemblyNames, Has.None.StartWith("UnityEngine"));
        }

        private static PitchDraft CreateCompleteDraft(
            MasteryState problem,
            MasteryState evidence,
            MasteryState solution,
            MasteryState value)
        {
            var draft = new PitchDraft();
            draft.TrySelectInitial(PitchPart.Problem, "problem", problem);
            draft.TrySelectInitial(PitchPart.Evidence, "evidence", evidence);
            draft.TrySelectInitial(PitchPart.Solution, "solution", solution);
            draft.TrySelectInitial(PitchPart.Value, "value", value);
            return draft;
        }
    }
}
