using NUnit.Framework;
using AudioFramework.Services.WallCheck;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the "should this wall-check loop run another iteration?" predicate. Every expected value is
    /// hand-derived from the agreed contract, NOT read off the implementation. If the implementation disagrees,
    /// the implementation is wrong, not the test. The contract, in order:
    ///   1. startGeneration != currentGeneration → false   (slot was reused for a different dispatch)
    ///   2. isPaused                             → true     (keep the loop alive across a pause)
    ///   3. isOneShot                            → isPlaying || currentTime &lt; busyUntilTime
    ///   4. otherwise (loop)                     → isPlaying
    /// Each test exercises exactly one clause, plus the busy-window boundary and the two generation-guard cases.
    /// </summary>
    public class WallCheckContinuationTests
    {
        [Test]
        public void GenerationMismatch_DoesNotContinue()
        {
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 1, currentGeneration: 2,
                isPaused: false, isOneShot: false, isPlaying: true,
                currentTime: 0f, busyUntilTime: 0f));
        }

        [Test]
        public void GenerationMismatch_OverridesPaused_DoesNotContinue()
        {
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 1, currentGeneration: 2,
                isPaused: true, isOneShot: false, isPlaying: false,
                currentTime: 0f, busyUntilTime: 0f));
        }

        [Test]
        public void Matched_Paused_Continues()
        {
            Assert.IsTrue(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: true, isOneShot: true, isPlaying: false,
                currentTime: 10f, busyUntilTime: 5f));
        }

        [Test]
        public void Loop_Playing_Continues()
        {
            Assert.IsTrue(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: false, isPlaying: true,
                currentTime: 0f, busyUntilTime: 0f));
        }

        [Test]
        public void Loop_NotPlaying_DoesNotContinue()
        {
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: false, isPlaying: false,
                currentTime: 0f, busyUntilTime: 0f));
        }

        [Test]
        public void OneShot_Playing_Continues()
        {
            Assert.IsTrue(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: true,
                currentTime: 10f, busyUntilTime: 5f));
        }

        [Test]
        public void OneShot_Silent_BusyWindowOpen_Continues()
        {
            Assert.IsTrue(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: false,
                currentTime: 3f, busyUntilTime: 5f));
        }

        [Test]
        public void OneShot_Silent_BusyWindowElapsed_DoesNotContinue()
        {
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: false,
                currentTime: 10f, busyUntilTime: 5f));
        }

        [Test]
        public void OneShot_Silent_CurrentTimeEqualsBusyUntil_DoesNotContinue()
        {
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: false,
                currentTime: 5f, busyUntilTime: 5f));
        }
    }
}
