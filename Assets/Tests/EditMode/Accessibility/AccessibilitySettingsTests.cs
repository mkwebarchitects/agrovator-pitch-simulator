using System;
using Agrovator.PitchSimulator.Accessibility;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Accessibility
{
    public sealed class AccessibilitySettingsTests
    {
        [Test]
        public void Constructor_PreservesSupportedSettings()
        {
            var settings = new AccessibilitySettings(
                TimerMode.Extended,
                true,
                0.25f,
                0.75f,
                "ms");

            Assert.That(settings.TimerMode, Is.EqualTo(TimerMode.Extended));
            Assert.That(settings.ReducedMotion, Is.True);
            Assert.That(settings.MusicVolume, Is.EqualTo(0.25f));
            Assert.That(settings.SfxVolume, Is.EqualTo(0.75f));
            Assert.That(settings.Locale, Is.EqualTo("ms"));
        }

        [TestCase(-0.1f, 0f)]
        [TestCase(1.1f, 1f)]
        [TestCase(float.NegativeInfinity, 0f)]
        [TestCase(float.PositiveInfinity, 1f)]
        public void Constructor_ClampsOutOfRangeVolumes(float input, float expected)
        {
            var settings = Create(musicVolume: input, sfxVolume: input);

            Assert.That(settings.MusicVolume, Is.EqualTo(expected));
            Assert.That(settings.SfxVolume, Is.EqualTo(expected));
        }

        [Test]
        public void Constructor_ConvertsNotANumberVolumesToSilence()
        {
            var settings = Create(musicVolume: float.NaN, sfxVolume: float.NaN);

            Assert.That(settings.MusicVolume, Is.Zero);
            Assert.That(settings.SfxVolume, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("fr")]
        public void Constructor_UsesEnglishForUnsupportedLocale(string locale)
        {
            var settings = Create(locale: locale);

            Assert.That(settings.Locale, Is.EqualTo("en"));
        }

        [Test]
        public void Constructor_UsesNormalForUnknownTimerMode()
        {
            var settings = Create(timerMode: (TimerMode)999);

            Assert.That(settings.TimerMode, Is.EqualTo(TimerMode.Normal));
        }

        [Test]
        public void GetEffectiveDuration_NormalUsesAuthoredDuration()
        {
            var settings = Create(timerMode: TimerMode.Normal);

            Assert.That(settings.GetEffectiveDuration(20d, false), Is.EqualTo(20d));
        }

        [Test]
        public void GetEffectiveDuration_ExtendedUsesOneAndAHalfTimesAuthoredDuration()
        {
            var settings = Create(timerMode: TimerMode.Extended);

            Assert.That(settings.GetEffectiveDuration(20d, false), Is.EqualTo(30d));
        }

        [Test]
        public void GetEffectiveDuration_OffReturnsZero()
        {
            var settings = Create(timerMode: TimerMode.Off);

            Assert.That(settings.GetEffectiveDuration(20d, false), Is.Zero);
        }

        [TestCase(TimerMode.Normal)]
        [TestCase(TimerMode.Extended)]
        [TestCase(TimerMode.Off)]
        public void GetEffectiveDuration_TutorialReturnsZero(TimerMode timerMode)
        {
            var settings = Create(timerMode: timerMode);

            Assert.That(settings.GetEffectiveDuration(20d, true), Is.Zero);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        public void GetEffectiveDuration_RejectsInvalidAuthoredDuration(double duration)
        {
            var settings = Create();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => settings.GetEffectiveDuration(duration, false));
        }

        private static AccessibilitySettings Create(
            TimerMode timerMode = TimerMode.Normal,
            bool reducedMotion = false,
            float musicVolume = 1f,
            float sfxVolume = 1f,
            string locale = "en")
        {
            return new AccessibilitySettings(
                timerMode,
                reducedMotion,
                musicVolume,
                sfxVolume,
                locale);
        }
    }
}
