using NUnit.Framework;
using AudioFramework.Core;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for handle currency: is an AudioHandle still pointing at the exact sound it was issued for?
    /// Two guards, both hand-derived from the agreed design:
    ///   • generation equality -> a handle goes stale the moment its slot is reused.
    ///   • bounds -> an index outside the pool is never current.
    /// If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class AudioHandleValidatorTests
    {

        [Test]
        public void IsCurrent_MatchingGenerationInRange_ReturnsTrue()
        {
            Assert.IsTrue(AudioHandleValidator.IsCurrent(handleIndex: 3, handleGeneration: 7, slotGeneration: 7, poolLength: 10));
        }

        [Test]
        public void IsCurrent_StaleGeneration_ReturnsFalse()
        {
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: 3, handleGeneration: 7, slotGeneration: 8, poolLength: 10));
        }


        [Test]
        public void IsCurrent_NegativeIndex_ReturnsFalse()
        {
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: -1, handleGeneration: 0, slotGeneration: 0, poolLength: 10));
        }

        [Test]
        public void IsCurrent_IndexAtPoolLength_ReturnsFalse()
        {
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: 10, handleGeneration: 0, slotGeneration: 0, poolLength: 10));
        }

        [Test]
        public void IsCurrent_IndexFarAbovePoolLength_ReturnsFalse()
        {
            Assert.IsFalse(AudioHandleValidator.IsCurrent(handleIndex: 99999, handleGeneration: 0, slotGeneration: 0, poolLength: 50));
        }

        [Test]
        public void IsCurrent_IndexZeroLowerBound_IsInRange()
        {
            Assert.IsTrue(AudioHandleValidator.IsCurrent(handleIndex: 0, handleGeneration: 2, slotGeneration: 2, poolLength: 10));
        }
    }
}
