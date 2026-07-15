using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using Agrovator.PitchSimulator.Accessibility;
using Agrovator.PitchSimulator.GuidedPitch;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.GuidedPitch
{
    public sealed class GuidedPitchContentTests
    {
        private static readonly IReadOnlyDictionary<string, ExpectedOption> ExpectedOptions =
            new Dictionary<string, ExpectedOption>(StringComparer.Ordinal)
            {
                ["primary-problem-clear"] = Expected(LearnerMode.Primary, PitchPart.Problem, MasteryState.Clear, "Our garden beds get too dry because we water them at the wrong times."),
                ["primary-problem-developing"] = Expected(LearnerMode.Primary, PitchPart.Problem, MasteryState.Developing, "Sometimes the garden is dry, and students carry watering cans after school."),
                ["primary-problem-needs-practice"] = Expected(LearnerMode.Primary, PitchPart.Problem, MasteryState.NeedsPractice, "Our smart garden will be the best school project anyone has ever seen."),
                ["primary-evidence-clear"] = Expected(LearnerMode.Primary, PitchPart.Evidence, MasteryState.Clear, "We saw dry soil on three days and puddles after it rained."),
                ["primary-evidence-developing"] = Expected(LearnerMode.Primary, PitchPart.Evidence, MasteryState.Developing, "Our team often saw dry soil and too much water near the beds."),
                ["primary-evidence-needs-practice"] = Expected(LearnerMode.Primary, PitchPart.Evidence, MasteryState.NeedsPractice, "The sensor must save lots of water because smart machines are always accurate."),
                ["primary-solution-clear"] = Expected(LearnerMode.Primary, PitchPart.Solution, MasteryState.Clear, "A sensor checks the soil, then waters only when the garden is dry."),
                ["primary-solution-developing"] = Expected(LearnerMode.Primary, PitchPart.Solution, MasteryState.Developing, "We will use a sensor, tank, tube, and valve in each bed."),
                ["primary-solution-needs-practice"] = Expected(LearnerMode.Primary, PitchPart.Solution, MasteryState.NeedsPractice, "The machine will do all the work, so students never need to check."),
                ["primary-value-clear"] = Expected(LearnerMode.Primary, PitchPart.Value, MasteryState.Clear, "It saves water, helps vegetables grow, and teaches students how sensors work."),
                ["primary-value-developing"] = Expected(LearnerMode.Primary, PitchPart.Value, MasteryState.Developing, "The garden gives the canteen vegetables and makes our school look modern."),
                ["primary-value-needs-practice"] = Expected(LearnerMode.Primary, PitchPart.Value, MasteryState.NeedsPractice, "The project matters because everyone will think our technology is very amazing."),
                ["intro-clear-problem"] = Expected(LearnerMode.Secondary, PitchPart.Problem, MasteryState.Clear, "We water on fixed schedules even when soil is wet, wasting water and weakening canteen crops."),
                ["intro-useful-dense"] = Expected(LearnerMode.Secondary, PitchPart.Problem, MasteryState.Developing, "Our logs show dry beds after assembly, wet beds after rain, and students carrying watering cans, so the timing is inconsistent."),
                ["intro-unsupported-claim"] = Expected(LearnerMode.Secondary, PitchPart.Problem, MasteryState.NeedsPractice, "Our invention will cut the school's water bill by 90% and produce enough vegetables for everyone."),
                ["evidence-standard-measured"] = Expected(LearnerMode.Secondary, PitchPart.Evidence, MasteryState.Clear, "In our two-week bed trial, sensor watering used 18% less water than the fixed schedule, while the plants stayed healthy."),
                ["evidence-standard-observation"] = Expected(LearnerMode.Secondary, PitchPart.Evidence, MasteryState.Developing, "The sensor usually stopped watering after rain, and our team noticed fewer puddles around the test bed."),
                ["evidence-standard-assumption"] = Expected(LearnerMode.Secondary, PitchPart.Evidence, MasteryState.NeedsPractice, "Sensors use precise readings, so they should always save more water than students using watering cans."),
                ["solution-practical-loop"] = Expected(LearnerMode.Secondary, PitchPart.Solution, MasteryState.Clear, "The sensor checks soil moisture, the valve waters a dry bed, and students review the plants and readings daily."),
                ["solution-components"] = Expected(LearnerMode.Secondary, PitchPart.Solution, MasteryState.Developing, "We connect a moisture sensor, controller, water tank, tubing, and a valve to each vegetable bed."),
                ["solution-automation-only"] = Expected(LearnerMode.Secondary, PitchPart.Solution, MasteryState.NeedsPractice, "The system automates watering from each sensor reading, so students can leave every decision to the controller and stop checking the beds once it is installed."),
                ["final-balanced-value"] = Expected(LearnerMode.Secondary, PitchPart.Value, MasteryState.Clear, "The garden saves water, teaches students through real sensor data, supplies useful canteen vegetables, and gives the school evidence for improving future beds."),
                ["final-vegetables-only"] = Expected(LearnerMode.Secondary, PitchPart.Value, MasteryState.Developing, "The garden can supply fresh vegetables for canteen meals and show visitors that our school can build a modern project."),
                ["final-tech-only"] = Expected(LearnerMode.Secondary, PitchPart.Value, MasteryState.NeedsPractice, "The main value is that our school will own an impressive smart sensor system with automated valves and modern monitoring."),
                ["primary-cost-clear"] = ExpectedFollowUp(LearnerMode.Primary, MasteryState.Clear, "First, we will price one test bed and ask the school before buying."),
                ["primary-cost-developing"] = ExpectedFollowUp(LearnerMode.Primary, MasteryState.Developing, "We think one bed may cost about RM500, but we need to check."),
                ["primary-cost-needs-practice"] = ExpectedFollowUp(LearnerMode.Primary, MasteryState.NeedsPractice, "It will be free because someone will surely donate everything we need."),
                ["cost-honest-next-step"] = ExpectedFollowUp(LearnerMode.Secondary, MasteryState.Clear, "We do not know the final cost. This week we will compare two local quotes for one pilot bed, including spare parts."),
                ["cost-rough-estimate"] = ExpectedFollowUp(LearnerMode.Secondary, MasteryState.Developing, "Our RM500 estimate covers the sensor, valve, tubing, and tank prices we found online, but not delivery, spare parts, installation, or confirmed local quotes."),
                ["cost-promise-free"] = ExpectedFollowUp(LearnerMode.Secondary, MasteryState.NeedsPractice, "It should cost nothing because local businesses will probably donate the sensors, tank, tubing, tools, and replacement parts when they hear about the project."),
            };

        [Test]
        public void AuthoredContent_LoadsExactStructureAndBothRoutesTerminate()
        {
            var content = LoadAuthoredContent();

            Assert.That(content.Id, Is.EqualTo("smart-school-garden"));
            Assert.That(content.Version, Is.EqualTo(2));
            Assert.That(content.EstimatedDurationMinutes, Is.InRange(8, 10));
            Assert.That(content.ContentChecksum, Is.EqualTo("guided-pitch-builder-v2-authored"));
            Assert.That(content.SupportedLocales, Is.EqualTo(new[] { "en", "ms" }));
            Assert.That(content.Modes.Keys, Is.EquivalentTo(new[] { LearnerMode.Primary, LearnerMode.Secondary }));

            foreach (var mode in content.Modes.Values)
            {
                Assert.That(mode.Parts.Select(part => part.Part), Is.EqualTo(PitchParts.Ordered));
                Assert.That(mode.Parts, Has.All.Matches<GuidedPitchPartContent>(part => part.Options.Count == 3));
                Assert.That(mode.FollowUp.Options.Count, Is.EqualTo(3));
                Assert.That(mode.Parts.SelectMany(part => part.Options).Concat(mode.FollowUp.Options).Count(), Is.EqualTo(15));
            }
        }

        [Test]
        public void AuthoredContent_PreservesExactStableIdsSentencesMasteryAndReactionTransport()
        {
            var catalog = LoadCatalog();
            var actual = EnumerateOptions(LoadAuthoredContent()).ToList();

            Assert.That(actual.Select(item => item.Option.Id), Is.Unique);
            Assert.That(actual.Select(item => item.Option.Id), Is.EquivalentTo(ExpectedOptions.Keys));
            foreach (var item in actual)
            {
                var expected = ExpectedOptions[item.Option.Id];
                Assert.That(item.Mode, Is.EqualTo(expected.Mode), item.Option.Id);
                Assert.That(item.Part, Is.EqualTo(expected.Part), item.Option.Id);
                Assert.That(item.Option.Mastery, Is.EqualTo(expected.Mastery), item.Option.Id);
                Assert.That(catalog.Resolve("en", item.Option.TextKey), Is.EqualTo(expected.Sentence), item.Option.Id);
                Assert.That(item.Option.LegacyConfidenceDelta, Is.EqualTo(ExpectedDelta(expected.Mastery)), item.Option.Id);
                Assert.That(item.Option.ReactionCue, Is.EqualTo(ExpectedReaction(expected.Mastery)), item.Option.Id);
            }
        }

        [Test]
        public void AuthoredContent_ResolvesEveryKeyAndUsesPerOptionFeedbackKeyPattern()
        {
            var catalog = LoadCatalog();
            var content = LoadAuthoredContent();
            var keys = EnumerateLocalizationKeys(content).ToList();

            Assert.That(keys, Has.None.Null.Or.Empty);
            Assert.That(keys, Has.All.Matches<string>(key => catalog.Resolve("en", key) != $"[[missing:{key}]]"));
            foreach (var option in EnumerateOptions(content).Select(item => item.Option))
            {
                Assert.That(option.Feedback.WorkedKey, Is.EqualTo($"guided.feedback.{option.Id}.worked"));
                Assert.That(option.Feedback.MissingKey, Is.EqualTo($"guided.feedback.{option.Id}.missing"));
                Assert.That(option.Feedback.ImproveKey, Is.EqualTo($"guided.feedback.{option.Id}.improve"));
                Assert.That(new[] { option.Feedback.WorkedKey, option.Feedback.MissingKey, option.Feedback.ImproveKey }, Is.Unique);
            }
        }

        [Test]
        public void AuthoredContent_HasOneOptionAtEachMasteryAndRequiredReadingLevels()
        {
            var catalog = LoadCatalog();
            var content = LoadAuthoredContent();

            foreach (var mode in content.Modes.Values)
            {
                foreach (var options in mode.Parts.Select(part => part.Options).Append(mode.FollowUp.Options))
                {
                    Assert.That(options.Select(option => option.Mastery), Is.EquivalentTo(new[]
                    {
                        MasteryState.Clear,
                        MasteryState.Developing,
                        MasteryState.NeedsPractice,
                    }));

                    foreach (var option in options)
                    {
                        var words = WordCount(catalog.Resolve("en", option.TextKey));
                        Assert.That(words, mode.Mode == LearnerMode.Primary ? Is.InRange(12, 16) : Is.AtMost(32), option.Id);
                    }
                }
            }
        }

        [Test]
        public void CompiledContent_CollectionsAreReadOnlyAndDetachedFromDtoArrays()
        {
            var dto = ReadDto();
            var values = EnglishValues();
            var result = GuidedPitchContentLoader.LoadWithLocalizationValues(Serialize(dto), values);
            var content = result.Content;
            dto.Modes[0].Parts[0].Options[0].Id = "mutated";

            Assert.That(content.Modes[LearnerMode.Primary].Parts[0].Options[0].Id, Is.Not.EqualTo("mutated"));
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<LearnerMode, GuidedLearnerModeContent>)content.Modes).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GuidedPitchPartContent>)content.Modes[LearnerMode.Primary].Parts).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<GuidedPitchOption>)content.Modes[LearnerMode.Primary].Parts[0].Options).Clear());
        }

        [TestCase("duplicate", "guided.id_duplicate")]
        [TestCase("missing-mode", "guided.mode_missing")]
        [TestCase("missing-part", "guided.part_order_invalid")]
        [TestCase("reordered-part", "guided.part_order_invalid")]
        [TestCase("invalid-mastery", "guided.mastery_invalid")]
        [TestCase("absent-feedback", "guided.feedback_key_missing")]
        [TestCase("wrong-option-count", "guided.option_count_invalid")]
        [TestCase("unknown-localization", "guided.localization_key_missing")]
        [TestCase("blank-checksum", "guided.checksum_missing")]
        [TestCase("primary-word-count", "guided.primary_word_count_invalid")]
        public void Validate_InvalidContentReturnsExpectedStructuredError(string mutation, string expectedCode)
        {
            var dto = ReadDto();
            var values = EnglishValues();
            Mutate(dto, values, mutation);

            var issues = GuidedPitchContentValidator.ValidateWithLocalizationValues(dto, values);
            var issue = issues.FirstOrDefault(item => item.Code == expectedCode);

            Assert.That(issue, Is.Not.Null, string.Join(", ", issues.Select(item => item.Code + "@" + item.Path)));
            Assert.That(issue.Severity, Is.EqualTo(GuidedPitchContentIssueSeverity.Error));
            Assert.That(issue.Path, Is.Not.Null.And.Not.Empty);
            Assert.That(typeof(GuidedPitchContentIssue).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name), Is.EquivalentTo(new[] { "Code", "Path", "Severity" }));
        }

        [Test]
        public void Validate_ReturnsAllIssuesInDocumentOrder()
        {
            var dto = ReadDto();
            var values = EnglishValues();
            dto.TitleKey = "missing.title";
            dto.BriefingKey = "missing.briefing";

            var issues = GuidedPitchContentValidator.ValidateWithLocalizationValues(dto, values)
                .Where(issue => issue.Code == "guided.localization_key_missing")
                .Take(2)
                .ToList();

            Assert.That(issues.Select(issue => issue.Path), Is.EqualTo(new[] { "TitleKey", "BriefingKey" }));
        }

        [Test]
        public void Validate_SecondarySentenceOver32WordsReturnsStructuredError()
        {
            var dto = ReadDto();
            var values = EnglishValues();
            var secondaryOption = dto.Modes.Single(mode => mode.Mode == "Secondary").Parts[0].Options[0];
            values[secondaryOption.TextKey] = string.Join(" ", Enumerable.Repeat("word", 33));

            var issue = GuidedPitchContentValidator.ValidateWithLocalizationValues(dto, values)
                .SingleOrDefault(item => item.Code == "guided.secondary_word_count_invalid");

            Assert.That(issue, Is.Not.Null);
            Assert.That(issue.Path, Is.EqualTo("Modes[1].Parts[0].Options[0].TextKey"));
            Assert.That(issue.Severity, Is.EqualTo(GuidedPitchContentIssueSeverity.Error));
        }

        [Test]
        public void Load_KeyOnlyContractRejectsValidationErrorsBeforeCompilation()
        {
            var json = ReadAuthoredJson().Replace(
                "guided-pitch-builder-v2-authored",
                "   ");

            var result = GuidedPitchContentLoader.Load(json, LoadCatalog().GetKeys("en"));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Content, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Code), Does.Contain("guided.checksum_missing"));
        }

        private static GuidedPitchContent LoadAuthoredContent()
        {
            var result = GuidedPitchContentLoader.LoadWithLocalizationValues(ReadAuthoredJson(), EnglishValues());
            Assert.That(result.IsSuccess, Is.True, string.Join(", ", result.Issues.Select(issue => issue.Code + "@" + issue.Path)));
            return result.Content;
        }

        private static GuidedPitchContentDto ReadDto()
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                Encoding.UTF8.GetBytes(ReadAuthoredJson()),
                XmlDictionaryReaderQuotas.Max))
            {
                return (GuidedPitchContentDto)new DataContractJsonSerializer(typeof(GuidedPitchContentDto)).ReadObject(reader);
            }
        }

        private static string Serialize(GuidedPitchContentDto dto)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(GuidedPitchContentDto)).WriteObject(stream, dto);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static void Mutate(
            GuidedPitchContentDto dto,
            IDictionary<string, string> values,
            string mutation)
        {
            var primary = dto.Modes[0];
            switch (mutation)
            {
                case "duplicate":
                    primary.Parts[0].Options[1].Id = primary.Parts[0].Options[0].Id;
                    break;
                case "missing-mode":
                    dto.Modes = new[] { primary };
                    break;
                case "missing-part":
                    primary.Parts = primary.Parts.Take(3).ToArray();
                    break;
                case "reordered-part":
                    var first = primary.Parts[0];
                    primary.Parts[0] = primary.Parts[1];
                    primary.Parts[1] = first;
                    break;
                case "invalid-mastery":
                    primary.Parts[0].Options[0].Mastery = "Expert";
                    break;
                case "absent-feedback":
                    primary.Parts[0].Options[0].Feedback.WorkedKey = null;
                    break;
                case "wrong-option-count":
                    primary.Parts[0].Options = primary.Parts[0].Options.Take(2).ToArray();
                    break;
                case "unknown-localization":
                    dto.TitleKey = "guided.unknown";
                    break;
                case "blank-checksum":
                    dto.ContentChecksum = "   ";
                    break;
                case "primary-word-count":
                    values[primary.Parts[0].Options[0].TextKey] = "Too short.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }
        }

        private static IEnumerable<(LearnerMode Mode, PitchPart? Part, GuidedPitchOption Option)> EnumerateOptions(
            GuidedPitchContent content)
        {
            foreach (var mode in content.Modes.Values)
            {
                foreach (var part in mode.Parts)
                {
                    foreach (var option in part.Options)
                    {
                        yield return (mode.Mode, part.Part, option);
                    }
                }

                foreach (var option in mode.FollowUp.Options)
                {
                    yield return (mode.Mode, null, option);
                }
            }
        }

        private static IEnumerable<string> EnumerateLocalizationKeys(GuidedPitchContent content)
        {
            yield return content.TitleKey;
            yield return content.BriefingKey;
            yield return content.LearnIncompletePitchKey;
            yield return content.LearnExplanationKey;
            foreach (var mode in content.Modes.Values)
            {
                yield return mode.PromptStyleKey;
                foreach (var part in mode.Parts)
                {
                    yield return part.LabelKey;
                    yield return part.PlainPromptKey;
                    yield return part.QuestionKey;
                    yield return part.HintKey;
                    foreach (var key in EnumerateOptionKeys(part.Options))
                    {
                        yield return key;
                    }
                }

                yield return mode.FollowUp.LabelKey;
                yield return mode.FollowUp.QuestionKey;
                yield return mode.FollowUp.HintKey;
                foreach (var key in EnumerateOptionKeys(mode.FollowUp.Options))
                {
                    yield return key;
                }
            }
        }

        private static IEnumerable<string> EnumerateOptionKeys(IEnumerable<GuidedPitchOption> options)
        {
            foreach (var option in options)
            {
                yield return option.TextKey;
                yield return option.Feedback.WorkedKey;
                yield return option.Feedback.MissingKey;
                yield return option.Feedback.ImproveKey;
            }
        }

        private static LocalizationCatalog LoadCatalog()
        {
            return LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
        }

        private static Dictionary<string, string> EnglishValues()
        {
            var catalog = LoadCatalog();
            return catalog.GetKeys("en").ToDictionary(key => key, key => catalog.Resolve("en", key), StringComparer.Ordinal);
        }

        private static string ReadAuthoredJson()
        {
            return File.ReadAllText(Path.Combine("Assets", "Content", "Scenarios", "guided-pitch-builder.en.json"));
        }

        private static string ReadCatalog(string locale)
        {
            return File.ReadAllText(Path.Combine("Assets", "Content", "Localization", locale + ".json"));
        }

        private static int WordCount(string value)
        {
            return value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static int ExpectedDelta(MasteryState mastery)
        {
            return mastery == MasteryState.Clear ? 4 : mastery == MasteryState.Developing ? 1 : -4;
        }

        private static string ExpectedReaction(MasteryState mastery)
        {
            return mastery == MasteryState.Clear ? "Impressed" : mastery == MasteryState.Developing ? "Curious" : "Concerned";
        }

        private static ExpectedOption Expected(
            LearnerMode mode,
            PitchPart part,
            MasteryState mastery,
            string sentence)
        {
            return new ExpectedOption(mode, part, mastery, sentence);
        }

        private static ExpectedOption ExpectedFollowUp(LearnerMode mode, MasteryState mastery, string sentence)
        {
            return new ExpectedOption(mode, null, mastery, sentence);
        }

        private sealed class ExpectedOption
        {
            public ExpectedOption(LearnerMode mode, PitchPart? part, MasteryState mastery, string sentence)
            {
                Mode = mode;
                Part = part;
                Mastery = mastery;
                Sentence = sentence;
            }

            public LearnerMode Mode { get; }
            public PitchPart? Part { get; }
            public MasteryState Mastery { get; }
            public string Sentence { get; }
        }
    }
}
