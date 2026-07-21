using System;
using System.Text;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;

namespace Agrovator.PitchSimulator.UI
{
    public readonly struct PitchPartVisual
    {
        public PitchPartVisual(PitchPart part, Color colour, string labelKey, string emptyPromptKey)
        {
            Part = part;
            Colour = colour;
            LabelKey = labelKey;
            EmptyPromptKey = emptyPromptKey;
        }

        public PitchPart Part { get; }
        public Color Colour { get; }
        public string LabelKey { get; }
        public string EmptyPromptKey { get; }
    }

    public static class PitchPartVisuals
    {
        public static readonly Color DeepNavy = Parse("#0E171F");
        public static readonly Color CardCream = Parse("#F4EAD5");
        public static readonly Color LightText = Parse("#FFF8E8");
        public static readonly Color CardText = Parse("#0E171F");
        public static readonly Color FocusGold = Parse("#FFD166");

        private static readonly PitchPartVisual[] Visuals =
        {
            Create(PitchPart.Problem, "#F28C6F", "problem"),
            Create(PitchPart.Evidence, "#67B7D1", "evidence"),
            Create(PitchPart.Solution, "#7BC47F", "solution"),
            Create(PitchPart.Value, "#E5B95C", "value"),
        };

        public static PitchPartVisual Get(PitchPart part)
        {
            if (part < PitchPart.Problem || part > PitchPart.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown pitch part.");
            }

            return Visuals[(int)part];
        }

        /// <summary>
        /// The single localization key for a mastery statement, shared by every
        /// guided screen that names Clear/Developing/Needs Practice.
        /// </summary>
        public static string MasteryLabelKey(MasteryState mastery)
        {
            switch (mastery)
            {
                case MasteryState.Clear:
                    return "guided.mastery.clear";
                case MasteryState.Developing:
                    return "guided.mastery.developing";
                case MasteryState.NeedsPractice:
                    return "guided.mastery.needs_practice";
                default:
                    throw new ArgumentOutOfRangeException(nameof(mastery), mastery, "Unknown mastery state.");
            }
        }

        /// <summary>
        /// Joins the localized CURRENT sentences of the populated draft sections
        /// with paragraph breaks in framework order. <paramref name="localize"/>
        /// must resolve response IDs through the composite localizer
        /// (response ID -> option TextKey -> catalog).
        /// </summary>
        public static string ComposeCurrentSentences(PitchDraftSnapshot draft, Func<string, string> localize)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            if (localize == null) throw new ArgumentNullException(nameof(localize));

            var builder = new StringBuilder();
            foreach (var part in PitchParts.Ordered)
            {
                var section = draft[part];
                if (!section.IsPopulated)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("\n\n");
                }

                builder.Append(localize(section.CurrentResponseId));
            }

            return builder.ToString();
        }

        public static float ContrastRatio(Color first, Color second)
        {
            var lighter = Mathf.Max(Luminance(first), Luminance(second));
            var darker = Mathf.Min(Luminance(first), Luminance(second));
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static PitchPartVisual Create(PitchPart part, string colour, string keyPart)
        {
            return new PitchPartVisual(
                part,
                Parse(colour),
                $"guided.part.{keyPart}.label",
                $"guided.board.add.{keyPart}");
        }

        private static Color Parse(string html)
        {
            if (!ColorUtility.TryParseHtmlString(html, out var colour))
            {
                throw new InvalidOperationException($"Invalid UI colour '{html}'.");
            }

            return colour;
        }

        private static float Luminance(Color colour)
        {
            return 0.2126f * Linear(colour.r) + 0.7152f * Linear(colour.g) + 0.0722f * Linear(colour.b);
        }

        private static float Linear(float channel)
        {
            return channel <= 0.03928f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }
    }
}
