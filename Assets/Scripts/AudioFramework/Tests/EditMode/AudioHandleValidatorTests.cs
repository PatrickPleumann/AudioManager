using NUnit.Framework;
using AudioFramework.Core;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for handle currency: is an AudioHandle still pointing at the exact sound it was issued for?
    /// Two guards, both hand-derived from the agreed design (NOT read off the implementation):
    ///   • generation equality → a handle goes stale the moment its slot is reused (W1).
    ///   • bounds → an index outside the pool is never current (P6: a hand-built {99999} must not crash anything).
    /// If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class AudioHandleValidatorTests
    {
        // --- Generation: the W1 core ---

        [Test]
        public void IsCurrent_MatchingGenerationInRange_ReturnsTrue()
        {
            Assert.IsTrue(AudioHandleValidator.IsCurrent(handleIndex: 3, handleGeneration: 7, slotGeneration: 7, poolLength: 10));
        }

        [Test]
        public void IsCurrent_StaleGeneration_ReturnsFalse()
        {
            // Same slot, but it was reused since the handle was issued (slot moved 7 -> 8): handle must be a no-op.
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: 3, handleGeneration: 7, slotGeneration: 8, poolLength: 10));
        }

        // --- Bounds: the P6 crash guard ---

        [Test]
        public void IsCurrent_NegativeIndex_ReturnsFalse()
        {
            // -1 is the "no slot" sentinel (e.g. Play() with CanHandleAudioSource == false).
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: -1, handleGeneration: 0, slotGeneration: 0, poolLength: 10));
        }

        [Test]
        public void IsCurrent_IndexAtPoolLength_ReturnsFalse()
        {
            // Off-by-one: a pool of length 10 has valid indices 0..9. Index 10 is out.
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: 10, handleGeneration: 0, slotGeneration: 0, poolLength: 10));
        }

        [Test]
        public void IsCurrent_IndexFarAbovePoolLength_ReturnsFalse()
        {
            // The literal P6 case: a hand-built {99999} handle must be rejected, not dereference the pool.
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: 99999, handleGeneration: 0, slotGeneration: 0, poolLength: 50));
        }

        [Test]
        public void IsCurrent_IndexZeroLowerBound_IsInRange()
        {
            // Boundary: index 0 is a valid slot, so currency rides on the generation alone.
            Assert.IsTrue(AudioHandleValidator.IsCurrent(handleIndex: 0, handleGeneration: 2, slotGeneration: 2, poolLength: 10));
        }
    }
}
