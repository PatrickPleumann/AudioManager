using NUnit.Framework;
using AudioFramework.Services.Fading;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the linear fade curve. Every expected value below is hand-derived from the agreed
    /// spec — NOT read off the implementation. If the implementation disagrees with these, the
    /// implementation is wrong, not the test.
    /// </summary>
    public class AudioFadeMathTests
    {
        private const float Delta = 1e-5f;

        // --- FadeIn shape: 0 -> 1 over 2 seconds ---

        [Test]
        public void Evaluate_AtStart_ReturnsFromValue()
        {
            Assert.AreEqual(0f, AudioFadeMath.Evaluate(from: 0f, to: 1f, elapsed: 0f, duration: 2f), Delta);
        }

        [Test]
        public void Evaluate_AtHalfDuration_ReturnsLinearMidpoint()
        {
            Assert.AreEqual(0.5f, AudioFadeMath.Evaluate(from: 0f, to: 1f, elapsed: 1f, duration: 2f), Delta);
        }

        [Test]
        public void Evaluate_AtEnd_ReturnsToValue()
        {
            Assert.AreEqual(1f, AudioFadeMath.Evaluate(from: 0f, to: 1f, elapsed: 2f, duration: 2f), Delta);
        }

        [Test]
        public void Evaluate_PastEnd_ClampsToTarget()
        {
            Assert.AreEqual(1f, AudioFadeMath.Evaluate(from: 0f, to: 1f, elapsed: 3f, duration: 2f), Delta);
        }

        [Test]
        public void Evaluate_BeforeStart_ClampsToFromValue()
        {
            Assert.AreEqual(0f, AudioFadeMath.Evaluate(from: 0f, to: 1f, elapsed: -1f, duration: 2f), Delta);
        }

        // --- Target is NOT hardcoded to 1: a category volume (e.g. 0.6) must be honored ---

        [Test]
        public void Evaluate_FadeIn_ToCategoryVolume_ScalesToThatTarget()
        {
            // Category volume = 0.6 (not 1). Halfway through a 2s fade => 0 -> 0.6 => 0.3
            Assert.AreEqual(0.3f, AudioFadeMath.Evaluate(from: 0f, to: 0.6f, elapsed: 1f, duration: 2f), Delta);
        }

        [Test]
        public void Evaluate_FadeIn_ToCategoryVolume_SettlesAtTarget()
        {
            Assert.AreEqual(0.6f, AudioFadeMath.Evaluate(from: 0f, to: 0.6f, elapsed: 2f, duration: 2f), Delta);
        }

        // --- FadeOut shape: 1 -> 0 over 4 seconds ---

        [Test]
        public void Evaluate_FadeOut_AtQuarterElapsed_RampsDown()
        {
            // 25% of the way through time => 25% of the way from 1 to 0 => 0.75 remaining
            Assert.AreEqual(0.75f, AudioFadeMath.Evaluate(from: 1f, to: 0f, elapsed: 1f, duration: 4f), Delta);
        }

        // --- Degenerate duration must not divide by zero ---

        [Test]
        public void Evaluate_ZeroDuration_ReturnsTargetInstantly()
        {
            Assert.AreEqual(1f, AudioFadeMath.Evaluate(from: 0f, to: 1f, elapsed: 0f, duration: 0f), Delta);
        }
    }
}
