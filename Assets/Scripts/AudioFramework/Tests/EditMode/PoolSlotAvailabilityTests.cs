using NUnit.Framework;
using AudioFramework.Pooling;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the "is this pool slot free for re-assignment?" predicate. Every expected value is hand-derived
    /// from the agreed contract (free ⟺ not playing AND OneShot busy-window elapsed AND not paused), NOT read
    /// off the implementation. If the implementation disagrees with these, the implementation is wrong, not the
    /// test. Each test exercises exactly one clause of the predicate, plus the busy-window boundary.
    /// </summary>
    public class PoolSlotAvailabilityTests
    {
        [Test]
        public void Silent_Elapsed_NotPaused_IsFree()
        {
            Assert.IsTrue(PoolSlotAvailability.IsFree(isPlaying: false, currentTime: 10f, busyUntilTime: 5f, isPaused: false));
        }

        [Test]
        public void Playing_IsNotFree()
        {
            Assert.IsFalse(PoolSlotAvailability.IsFree(isPlaying: true, currentTime: 10f, busyUntilTime: 5f, isPaused: false));
        }

        [Test]
        public void BusyWindowNotElapsed_IsNotFree()
        {
            Assert.IsFalse(PoolSlotAvailability.IsFree(isPlaying: false, currentTime: 3f, busyUntilTime: 5f, isPaused: false));
        }

        [Test]
        public void Paused_IsNotFree_EvenWhenOtherwiseFree()
        {
            // Silent + busy-window elapsed, but we paused this slot: it must stay occupied (a paused source
            // reports isPlaying == false, so the pause guard is the only thing keeping it from being reused).
            Assert.IsFalse(PoolSlotAvailability.IsFree(isPlaying: false, currentTime: 10f, busyUntilTime: 5f, isPaused: true));
        }

        [Test]
        public void CurrentTimeEqualsBusyUntil_IsFree()
        {
            // Boundary: "busy-window elapsed" is inclusive — currentTime >= busyUntilTime, so equality is free.
            Assert.IsTrue(PoolSlotAvailability.IsFree(isPlaying: false, currentTime: 5f, busyUntilTime: 5f, isPaused: false));
        }
    }
}
