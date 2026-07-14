using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Agrovator.PitchSimulator.Accessibility;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Accessibility
{
    public sealed class LocalizationTests
    {
        private static readonly string[] RequiredResultKeys =
        {
            "result.level.seedling",
            "result.level.sprouting",
            "result.level.growing",
            "result.level.thriving",
            "result.strength.clear_explanation",
            "result.strength.problem",
            "result.strength.solution",
            "result.strength.audience",
            "result.strength.evidence",
            "result.strength.communication",
            "result.strength.time_management",
            "result.strength.recovery",
            "result.improvement.clear_explanation",
            "result.improvement.problem",
            "result.improvement.solution",
            "result.improvement.audience",
            "result.improvement.evidence",
            "result.improvement.communication",
            "result.improvement.time_management",
        };

        [Test]
        public void ReviewedEnglishCatalog_ResolvesEveryDeclaredKey()
        {
            var catalog = LoadAuthoredCatalogs();

            foreach (var key in catalog.GetKeys("en"))
            {
                Assert.That(catalog.Resolve("en", key), Is.Not.Null.And.Not.Empty, key);
                Assert.That(catalog.Resolve("en", key), Does.Not.StartWith("[[missing:"), key);
            }

            Assert.That(catalog.GetTranslationStatus("en"), Is.EqualTo("reviewed"));
        }

        [Test]
        public void PendingMalayCatalog_HasExactEnglishKeyParity()
        {
            var catalog = LoadAuthoredCatalogs();

            Assert.That(catalog.GetKeys("ms"), Is.EquivalentTo(catalog.GetKeys("en")));
            Assert.That(catalog.GetTranslationStatus("ms"), Is.EqualTo("pending_human_review"));
        }

        [Test]
        public void EnglishCatalog_ContainsEveryCurrentResultAndMinimalUiKey()
        {
            var keys = LoadAuthoredCatalogs().GetKeys("en");

            Assert.That(keys, Is.SupersetOf(RequiredResultKeys));
            Assert.That(keys, Is.SupersetOf(new[]
            {
                "ui.game_title",
                "ui.start",
                "ui.continue",
                "ui.retry",
                "ui.results",
                "ui.score",
                "ui.confidence",
                "ui.timer",
                "diagnostic.localization_missing",
            }));
        }

        [Test]
        public void Resolve_MissingLocaleFallsBackToEnglish()
        {
            var catalog = LoadAuthoredCatalogs();

            Assert.That(catalog.Resolve("fr", "ui.start"), Is.EqualTo(catalog.Resolve("en", "ui.start")));
        }

        [Test]
        public void Resolve_MissingKeyReturnsVisibleDeterministicTokenContainingOnlyKey()
        {
            var catalog = LoadAuthoredCatalogs();

            Assert.That(catalog.Resolve("ms", "ui.unknown"), Is.EqualTo("[[missing:ui.unknown]]"));
        }

        [Test]
        public void Load_UsesOrdinalCaseSensitiveLocaleAndKeyIdentity()
        {
            var catalog = LocalizationCatalog.Load(
                CatalogJson("en", "reviewed", ("ui.start", "Start"), ("UI.START", "Upper")),
                CatalogJson("EN", "reviewed", ("ui.start", "Other locale")));

            Assert.That(catalog.Resolve("en", "ui.start"), Is.EqualTo("Start"));
            Assert.That(catalog.Resolve("en", "UI.START"), Is.EqualTo("Upper"));
            Assert.That(catalog.Resolve("EN", "ui.start"), Is.EqualTo("Other locale"));
        }

        [Test]
        public void Load_RejectsDuplicateKeysInsteadOfOverwriting()
        {
            var json = CatalogJson("en", "reviewed", ("ui.start", "Start"), ("ui.start", "Again"));

            Assert.Throws<FormatException>(() => LocalizationCatalog.Load(json));
        }

        [TestCase("{\"locale\":\"en\",\"translationStatus\":\"reviewed\",\"entries\":null}")]
        [TestCase("{\"locale\":\"en\",\"translationStatus\":\"reviewed\",\"entries\":[null]}")]
        [TestCase("{\"locale\":\"en\",\"translationStatus\":\"reviewed\",\"entries\":[{\"key\":null,\"value\":\"Start\"}]}")]
        [TestCase("{\"locale\":\"en\",\"translationStatus\":\"reviewed\",\"entries\":[{\"key\":\"ui.start\",\"value\":null}]}")]
        public void Load_RejectsMalformedOrNullEntries(string json)
        {
            Assert.Throws<FormatException>(() => LocalizationCatalog.Load(json));
        }

        [Test]
        public void Load_RejectsMalformedJson()
        {
            Assert.Throws<FormatException>(() => LocalizationCatalog.Load("{not-json}"));
        }

        private static LocalizationCatalog LoadAuthoredCatalogs()
        {
            return LocalizationCatalog.Load(ReadCatalog("en"), ReadCatalog("ms"));
        }

        private static string ReadCatalog(string locale)
        {
            return File.ReadAllText(Path.Combine("Assets", "Content", "Localization", locale + ".json"));
        }

        private static string CatalogJson(
            string locale,
            string translationStatus,
            params (string Key, string Value)[] entries)
        {
            var serializedEntries = string.Join(",", entries.Select(entry =>
                $"{{\"key\":\"{entry.Key}\",\"value\":\"{entry.Value}\"}}"));
            return $"{{\"locale\":\"{locale}\",\"translationStatus\":\"{translationStatus}\",\"entries\":[{serializedEntries}]}}";
        }
    }
}
