using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Agrovator.PitchSimulator.GuidedPitch
{
    public sealed class PitchSectionSnapshot
    {
        internal PitchSectionSnapshot(
            PitchPart part,
            string initialResponseId,
            string currentResponseId,
            MasteryState? initialMastery,
            MasteryState? currentMastery,
            bool wasRevised)
        {
            Part = part;
            InitialResponseId = initialResponseId;
            CurrentResponseId = currentResponseId;
            InitialMastery = initialMastery;
            CurrentMastery = currentMastery;
            WasRevised = wasRevised;
        }

        public PitchPart Part { get; }

        public string InitialResponseId { get; }

        public string CurrentResponseId { get; }

        public MasteryState? InitialMastery { get; }

        public MasteryState? CurrentMastery { get; }

        public bool IsPopulated => CurrentMastery.HasValue;

        public bool WasRevised { get; }
    }

    public sealed class PitchDraftSnapshot
    {
        private readonly IReadOnlyDictionary<PitchPart, PitchSectionSnapshot> sections;

        internal PitchDraftSnapshot(IEnumerable<PitchSectionSnapshot> sections)
        {
            var copies = sections.ToDictionary(section => section.Part);
            this.sections = new ReadOnlyDictionary<PitchPart, PitchSectionSnapshot>(copies);
            PopulatedCount = copies.Values.Count(section => section.IsPopulated);
        }

        public PitchSectionSnapshot this[PitchPart part]
        {
            get
            {
                PitchDraft.ValidatePart(part);
                return sections[part];
            }
        }

        public bool IsComplete => PopulatedCount == PitchParts.Ordered.Count;

        public int PopulatedCount { get; }
    }

    public sealed class PitchDraft
    {
        private readonly MutablePitchSection[] sections;

        public PitchDraft()
        {
            sections = PitchParts.Ordered
                .Select(part => new MutablePitchSection(part))
                .ToArray();
        }

        public PitchDraftSnapshot Snapshot => new PitchDraftSnapshot(
            sections.Select(section => section.CreateSnapshot()));

        public bool TrySelectInitial(PitchPart part, string responseId, MasteryState mastery)
        {
            ValidatePart(part);
            ValidateResponseId(responseId);
            ValidateMastery(mastery);

            var section = sections[(int)part];
            if (section.IsPopulated)
            {
                return false;
            }

            section.InitialResponseId = responseId;
            section.CurrentResponseId = responseId;
            section.InitialMastery = mastery;
            section.CurrentMastery = mastery;
            return true;
        }

        public bool TryRevise(PitchPart part, string responseId, MasteryState mastery)
        {
            ValidatePart(part);
            ValidateResponseId(responseId);
            ValidateMastery(mastery);

            var section = sections[(int)part];
            if (!section.IsPopulated)
            {
                return false;
            }

            section.CurrentResponseId = responseId;
            section.CurrentMastery = mastery;
            section.WasRevised = true;
            return true;
        }

        public void Reset()
        {
            foreach (var section in sections)
            {
                section.Reset();
            }
        }

        internal static void ValidatePart(PitchPart part)
        {
            if (part < PitchPart.Problem || part > PitchPart.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown pitch part.");
            }
        }

        private static void ValidateResponseId(string responseId)
        {
            if (string.IsNullOrWhiteSpace(responseId))
            {
                throw new ArgumentException("A response ID is required.", nameof(responseId));
            }
        }

        private static void ValidateMastery(MasteryState mastery)
        {
            if (mastery < MasteryState.NeedsPractice || mastery > MasteryState.Clear)
            {
                throw new ArgumentOutOfRangeException(nameof(mastery), mastery, "Unknown mastery state.");
            }
        }

        private sealed class MutablePitchSection
        {
            internal MutablePitchSection(PitchPart part)
            {
                Part = part;
            }

            internal PitchPart Part { get; }

            internal string InitialResponseId { get; set; }

            internal string CurrentResponseId { get; set; }

            internal MasteryState? InitialMastery { get; set; }

            internal MasteryState? CurrentMastery { get; set; }

            internal bool WasRevised { get; set; }

            internal bool IsPopulated => CurrentMastery.HasValue;

            internal PitchSectionSnapshot CreateSnapshot()
            {
                return new PitchSectionSnapshot(
                    Part,
                    InitialResponseId,
                    CurrentResponseId,
                    InitialMastery,
                    CurrentMastery,
                    WasRevised);
            }

            internal void Reset()
            {
                InitialResponseId = null;
                CurrentResponseId = null;
                InitialMastery = null;
                CurrentMastery = null;
                WasRevised = false;
            }
        }
    }
}
