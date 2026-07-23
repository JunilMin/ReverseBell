using System;
using NUnit.Framework;
using ReverseBell.Core;

namespace ReverseBell.Tests
{
    public sealed class BeatTimelineTests
    {
        [Test]
        public void SecondsPerBeat_At120Bpm_IsHalfASecond()
        {
            var timeline = new BeatTimeline(120d, 4, 10d);

            Assert.That(timeline.SecondsPerBeat, Is.EqualTo(0.5d));
        }

        [TestCase(9.99d, -1)]
        [TestCase(10d, 0)]
        [TestCase(10.49d, 0)]
        [TestCase(10.5d, 1)]
        [TestCase(12d, 4)]
        public void GetBeatIndex_ReturnsExpectedBeat(double dspTime, long expectedBeat)
        {
            var timeline = new BeatTimeline(120d, 4, 10d);

            Assert.That(timeline.GetBeatIndex(dspTime), Is.EqualTo(expectedBeat));
        }

        [Test]
        public void GetBarIndex_AdvancesEveryFourBeats()
        {
            var timeline = new BeatTimeline(120d, 4, 10d);

            Assert.That(timeline.GetBarIndex(11.99d), Is.EqualTo(0));
            Assert.That(timeline.GetBarIndex(12d), Is.EqualTo(1));
        }

        [Test]
        public void Constructor_WithInvalidBpm_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BeatTimeline(0d, 4, 0d));
        }
    }
}
