using NUnit.Framework;
using AudioFramework.Services.Fading;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for fade progress over time. Expected values are hand-derived from the agreed spec, NOT read off
    /// the implementation. FadeOperation is immutable: Advanced() returns a new operation, so each test composes
    /// calls and inspects the result.
    /// </summary>
    public class FadeOperationTests
    {
        private const float Delta = 1e-5f;

        [Test]
        public void NewOperation_AtStart_VolumeIsFromValue()
        {
            var op = new FadeOperation(from: 0f, to: 1f, duration: 2f);
            Assert.AreEqual(0f, op.CurrentVolume, Delta);
        }

        [Test]
        public void NewOperation_IsNotComplete()
        {
            var op = new FadeOperation(from: 0f, to: 1f, duration: 2f);
            Assert.IsFalse(op.IsComplete);
        }

        [Test]
        public void Advanced_AccumulatesElapsedAcrossMultipleCalls()
        {
            // Two 0.5s ticks => 1.0s elapsed => halfway through a 2s fade => 0.5
            var op = new FadeOperation(0f, 1f, 2f).Advanced(0.5f).Advanced(0.5f);
            Assert.AreEqual(0.5f, op.CurrentVolume, Delta);
        }

        [Test]
        public void Advanced_ToHalfway_VolumeIsMidpoint_AndNotComplete()
        {
            var op = new FadeOperation(0f, 1f, 2f).Advanced(1f);
            Assert.AreEqual(0.5f, op.CurrentVolume, Delta);
            Assert.IsFalse(op.IsComplete);
        }

        [Test]
        public void Advanced_ToFullDuration_VolumeIsTo_AndComplete()
        {
            var op = new FadeOperation(0f, 1f, 2f).Advanced(2f);
            Assert.AreEqual(1f, op.CurrentVolume, Delta);
            Assert.IsTrue(op.IsComplete);
        }

        [Test]
        public void Advanced_PastDuration_VolumeStaysAtTo_AndComplete()
        {
            var op = new FadeOperation(0f, 1f, 2f).Advanced(5f);
            Assert.AreEqual(1f, op.CurrentVolume, Delta);
            Assert.IsTrue(op.IsComplete);
        }

        [Test]
        public void FadeOut_Advanced_RampsDown()
        {
            // 1 -> 0 over 4s, after 1s elapsed => 0.75 remaining
            var op = new FadeOperation(1f, 0f, 4f).Advanced(1f);
            Assert.AreEqual(0.75f, op.CurrentVolume, Delta);
        }

        [Test]
        public void ZeroDuration_IsCompleteImmediately_AtTarget()
        {
            var op = new FadeOperation(0f, 1f, 0f);
            Assert.IsTrue(op.IsComplete);
            Assert.AreEqual(1f, op.CurrentVolume, Delta);
        }
    }
}
