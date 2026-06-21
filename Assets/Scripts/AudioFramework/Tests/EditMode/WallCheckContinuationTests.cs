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
            // Slot was handed to a different dispatch (1 != 2). Even though a playing loop would otherwise
            // continue, the orphaned loop must stop. This is the R3 fix.
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 1, currentGeneration: 2,
                isPaused: false, isOneShot: false, isPlaying: true,
                currentTime: 0f, busyUntilTime: 0f));
        }

        [Test]
        public void GenerationMismatch_OverridesPaused_DoesNotContinue()
        {
            // Locks the clause ORDER: a reused slot may be paused for its NEW sound, but the old loop must
            // still die. Generation guard sits above the pause check.
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 1, currentGeneration: 2,
                isPaused: true, isOneShot: false, isPlaying: false,
                currentTime: 0f, busyUntilTime: 0f));
        }

        [Test]
        public void Matched_Paused_Continues()
        {
            // Same dispatch (5 == 5) and paused: keep the loop alive even though the OneShot is silent and its
            // busy-window has elapsed — pause wins over the playback clauses.
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
            // Playing → continue regardless of the busy window.
            Assert.IsTrue(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: true,
                currentTime: 10f, busyUntilTime: 5f));
        }

        [Test]
        public void OneShot_Silent_BusyWindowOpen_Continues()
        {
            // Silent but the OneShot busy-window is still open (3 < 5) → continue.
            Assert.IsTrue(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: false,
                currentTime: 3f, busyUntilTime: 5f));
        }

        [Test]
        public void OneShot_Silent_BusyWindowElapsed_DoesNotContinue()
        {
            // Silent and the busy-window has elapsed (10 >= 5) → stop.
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: false,
                currentTime: 10f, busyUntilTime: 5f));
        }

        [Test]
        public void OneShot_Silent_CurrentTimeEqualsBusyUntil_DoesNotContinue()
        {
            // Boundary: the busy window is strict (currentTime < busyUntilTime), so equality is NOT open →
            // stop. Mirrors the OneShot busy semantics and PoolSlotAvailability treating equality as free.
            Assert.IsFalse(WallCheckContinuation.ShouldContinue(
                startGeneration: 5, currentGeneration: 5,
                isPaused: false, isOneShot: true, isPlaying: false,
                currentTime: 5f, busyUntilTime: 5f));
        }
    }
}
