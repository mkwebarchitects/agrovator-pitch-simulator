using System;
using System.Linq;
using Agrovator.PitchSimulator.Core;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Core
{
    public sealed class SaveDataTests
    {
        [Test]
        public void CurrentVersion_RoundTripsEveryPreference()
        {
            var source = new SaveData
            {
                Version = SaveDataMigrator.CurrentVersion,
                TimerMode = 2,
                ReducedMotion = true,
                MusicVolume = 0.25f,
                SfxVolume = 0.75f,
                Locale = "en",
            };

            var copy = SaveDataMigrator.DeserializeAndMigrate(SaveDataMigrator.Serialize(source));

            Assert.That(copy.Version, Is.EqualTo(1));
            Assert.That(copy.TimerMode, Is.EqualTo(2));
            Assert.That(copy.ReducedMotion, Is.True);
            Assert.That(copy.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(copy.SfxVolume, Is.EqualTo(0.75f));
            Assert.That(copy.Locale, Is.EqualTo("en"));
        }

        [Test]
        public void VersionZero_MigratesToDeterministicVersionOneDefaults()
        {
            var migrated = SaveDataMigrator.DeserializeAndMigrate("{\"version\":0}");

            Assert.That(migrated.Version, Is.EqualTo(1));
            Assert.That(migrated.TimerMode, Is.Zero);
            Assert.That(migrated.ReducedMotion, Is.False);
            Assert.That(migrated.MusicVolume, Is.EqualTo(1f));
            Assert.That(migrated.SfxVolume, Is.EqualTo(1f));
            Assert.That(migrated.Locale, Is.EqualTo("en"));
        }

        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(int.MaxValue)]
        public void DeserializeAndMigrate_RejectsNegativeOrFutureVersions(int version)
        {
            Assert.Throws<NotSupportedException>(
                () => SaveDataMigrator.DeserializeAndMigrate($"{{\"version\":{version}}}"));
        }

        [Test]
        public void DeserializeAndMigrate_AcceptsSingleDocumentFollowedByWhitespace()
        {
            var json = SaveDataMigrator.Serialize(SaveDataMigrator.CreateDefault()) + " \r\n\t";

            Assert.That(
                SaveDataMigrator.DeserializeAndMigrate(json).Version,
                Is.EqualTo(SaveDataMigrator.CurrentVersion));
        }

        [Test]
        public void DeserializeAndMigrate_RejectsConcatenatedJsonDocument()
        {
            var json = SaveDataMigrator.Serialize(SaveDataMigrator.CreateDefault()) + "{}";

            Assert.Throws<FormatException>(() => SaveDataMigrator.DeserializeAndMigrate(json));
        }

        [Test]
        public void DeserializeAndMigrate_RejectsTrailingNonWhitespace()
        {
            var json = SaveDataMigrator.Serialize(SaveDataMigrator.CreateDefault()) + "junk";

            Assert.Throws<FormatException>(() => SaveDataMigrator.DeserializeAndMigrate(json));
        }

        [Test]
        public void SaveData_ContainsOnlyApprovedSettingsFields()
        {
            var approvedFields = new[]
            {
                "Version",
                "TimerMode",
                "ReducedMotion",
                "MusicVolume",
                "SfxVolume",
                "Locale",
            };
            var actualFields = typeof(SaveData).GetFields().Select(field => field.Name);

            Assert.That(actualFields, Is.EquivalentTo(approvedFields));
        }

        [Test]
        public void SaveData_DoesNotReferenceAccessibilityOrUnityEngine()
        {
            var fieldTypes = typeof(SaveData).GetFields().Select(field => field.FieldType).ToArray();

            Assert.That(fieldTypes.All(type => type.IsPrimitive || type == typeof(string)), Is.True);
            Assert.That(fieldTypes.Select(type => type.Namespace), Has.None.EqualTo("Agrovator.PitchSimulator.Accessibility"));
            Assert.That(fieldTypes.Select(type => type.Namespace), Has.None.EqualTo("UnityEngine"));
        }

        [Test]
        public void Serialize_RejectsUnsupportedVersion()
        {
            var data = SaveDataMigrator.CreateDefault();
            data.Version = SaveDataMigrator.CurrentVersion + 1;

            Assert.Throws<NotSupportedException>(() => SaveDataMigrator.Serialize(data));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void Serialize_RejectsUnknownTimerModes(int timerMode)
        {
            var data = SaveDataMigrator.CreateDefault();
            data.TimerMode = timerMode;

            Assert.Throws<ArgumentException>(() => SaveDataMigrator.Serialize(data));
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.PositiveInfinity)]
        public void Serialize_RejectsInvalidVolumes(float volume)
        {
            var data = SaveDataMigrator.CreateDefault();
            data.MusicVolume = volume;

            Assert.Throws<ArgumentException>(() => SaveDataMigrator.Serialize(data));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("fr")]
        public void Serialize_RejectsUnsupportedLocale(string locale)
        {
            var data = SaveDataMigrator.CreateDefault();
            data.Locale = locale;

            Assert.Throws<ArgumentException>(() => SaveDataMigrator.Serialize(data));
        }
    }
}
