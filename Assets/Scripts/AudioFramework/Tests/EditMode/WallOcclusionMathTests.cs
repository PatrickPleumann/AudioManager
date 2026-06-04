using NUnit.Framework;
using AudioFramework.Services.WallCheck;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-wall occlusion math, the swappable seam both wall-check services share.
    /// Every expected value is hand-derived from the agreed LINEAR model (a wall subtracts its layer
    /// reduction in Hz from the running cutoff), NOT read off the implementation. When the model later
    /// becomes logarithmic/multiplicative these expectations change deliberately — that is the whole
    /// point of isolating the step here. If the implementation disagrees with these, it is wrong.
    /// </summary>
    public class WallOcclusionMathTests
    {
        private const float Delta = 1e-5f;

        // --- Per-wall step: linear Hz subtraction from the open cutoff ---

        [Test]
        public void ApplyWall_SubtractsReductionFromCutoff()
        {
            Assert.AreEqual(8000f, WallOcclusionMath.ApplyWall(currentCutoff: 22000f, layerReduction: 14000f), Delta);
        }

        [Test]
        public void ApplyWall_AccumulatesAcrossWalls()
        {
            // Two walls of 10000 each, applied in sequence from the open cutoff: 22000 -> 12000 -> 2000.
            float afterFirst = WallOcclusionMath.ApplyWall(22000f, 10000f);
            Assert.AreEqual(2000f, WallOcclusionMath.ApplyWall(afterFirst, 10000f), Delta);
        }

        // --- Floor clamp: cutoff never drops below the configured minimum ---

        [Test]
        public void ClampToFloor_BelowFloor_ReturnsFloor()
        {
            Assert.AreEqual(100f, WallOcclusionMath.ClampToFloor(cutoff: 50f, minCutoff: 100f), Delta);
        }

        [Test]
        public void ClampToFloor_AboveFloor_ReturnsValueUnchanged()
        {
            Assert.AreEqual(8000f, WallOcclusionMath.ClampToFloor(cutoff: 8000f, minCutoff: 100f), Delta);
        }
    }
}
