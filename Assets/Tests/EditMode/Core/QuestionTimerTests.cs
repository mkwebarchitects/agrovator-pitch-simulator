using System;
using Agrovator.PitchSimulator.Core;
using NUnit.Framework;

namespace Agrovator.PitchSimulator.Tests.EditMode.Core
{
    public sealed class QuestionTimerTests
    {
        [Test]
        public void Constructor_StartsAtConfiguredDuration()
        {
            var timer = new QuestionTimer(30d);

            Assert.That(timer.RemainingSeconds, Is.EqualTo(30d));
            Assert.That(timer.IsPaused, Is.False);
            Assert.That(timer.HasExpired, Is.False);
        }

        [Test]
        public void Tick_DecreasesRemainingDuration()
        {
            var timer = new QuestionTimer(30d);

            timer.Tick(1.25d);

            Assert.That(timer.RemainingSeconds, Is.EqualTo(28.75d));
        }

        [Test]
        public void Pause_PreventsCountdown()
        {
            var timer = new QuestionTimer(30d);

            timer.Pause();
            timer.Tick(5d);

            Assert.That(timer.IsPaused, Is.True);
            Assert.That(timer.RemainingSeconds, Is.EqualTo(30d));
        }

        [Test]
        public void Resume_RestartsCountdown()
        {
            var timer = new QuestionTimer(30d);
            timer.Pause();

            timer.Resume();
            timer.Tick(5d);

            Assert.That(timer.IsPaused, Is.False);
            Assert.That(timer.RemainingSeconds, Is.EqualTo(25d));
        }

        [Test]
        public void Tick_ClampsAtZeroAndExpiresOnce()
        {
            var timer = new QuestionTimer(2d);
            var expiryCount = 0;
            timer.Expired += () => expiryCount++;

            timer.Tick(3d);
            timer.Tick(1d);
            timer.Tick(0d);

            Assert.That(timer.RemainingSeconds, Is.Zero);
            Assert.That(timer.HasExpired, Is.True);
            Assert.That(expiryCount, Is.EqualTo(1));
        }

        [Test]
        public void ZeroDuration_DisablesCountdownWithoutExpiring()
        {
            var timer = new QuestionTimer(0d);
            var expiryCount = 0;
            timer.Expired += () => expiryCount++;

            timer.Tick(10d);

            Assert.That(timer.RemainingSeconds, Is.Zero);
            Assert.That(timer.HasExpired, Is.False);
            Assert.That(expiryCount, Is.Zero);
        }

        [Test]
        public void NegativeTick_ThrowsBeforeMutation()
        {
            var timer = new QuestionTimer(30d);
            var expiryCount = 0;
            timer.Expired += () => expiryCount++;

            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(-1d));
            Assert.That(timer.RemainingSeconds, Is.EqualTo(30d));
            Assert.That(timer.HasExpired, Is.False);
            Assert.That(expiryCount, Is.Zero);
        }

        [TestCase(-1d)]
        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        public void Constructor_RejectsInvalidDuration(double duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new QuestionTimer(duration));
        }

        [TestCase(double.NaN)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(double.PositiveInfinity)]
        public void NonFiniteTick_ThrowsBeforeMutation(double seconds)
        {
            var timer = new QuestionTimer(30d);

            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(seconds));
            Assert.That(timer.RemainingSeconds, Is.EqualTo(30d));
            Assert.That(timer.HasExpired, Is.False);
        }

        [Test]
        public void Tick_DoesNotAllocate()
        {
            var timer = new QuestionTimer(10000d);
            timer.Tick(0d);
            var bytesBefore = GC.GetAllocatedBytesForCurrentThread();

            for (var index = 0; index < 1000; index++)
            {
                timer.Tick(0.001d);
            }

            var bytesAfter = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(bytesAfter, Is.EqualTo(bytesBefore));
        }
    }
}
