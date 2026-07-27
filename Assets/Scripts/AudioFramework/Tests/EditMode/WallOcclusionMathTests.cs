using NUnit.Framework;
using AudioFramework.Services.WallCheck;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-wall occlusion math, the swappable seam both wall-check services share.
    /// Every expected value is hand-derived from the agreed MULTIPLICATIVE model, NOT read off the
    /// implementation:
    ///   ApplyWall(current, floor, d) = current − (current − floor) · d
    /// A wall damps the running cutoff TOWARD the floor by its damping factor d (0 = transparent,
    /// 1 = drops to the floor in this one wall). Across walls the open range above the floor is scaled
    /// by ∏(1 − dᵢ): order-independent and asymptotic to (never below) the floor. Open cutoff = 22000,
    /// floor = 1000 throughout. If the implementation disagrees with these, it is wrong.
    /// </summary>
    public class WallOcclusionMathTests
    {
        // Tolerance derived from the AUDIO domain, not float32 mechanics. ApplyWall returns a low-pass
        // cutoff in Hz; the perceptual JND at these cutoffs is on the order of tens of Hz, so even 1 Hz
        // is inaudible. 1e-2 Hz is therefore perceptually exact, yet:
        //   • catches every meaningful mutation — those miss by hundreds-to-thousands of Hz (≥10000× this), and
        //   • stays robust: the 0.3f/0.8f chain lands ~2.4e-4 Hz off (~40× margin); a mathematically-equivalent
        //     reformulation differs only ~1 ULP — also covered.
        // We deliberately do NOT assert bit-exact equality: that would pin THIS arithmetic form and false-red
        // an equivalent refactor (e.g. floor + (current-floor)*(1-d)) — a change-detector, not a contract test.
        private const float Delta = 1e-2f;

        private const float Open = 22000f;
        private const float Floor = 1000f;

        [Test]
        public void ApplyWall_DampsTowardFloorByFraction()
        {
            Assert.AreEqual(11500f, WallOcclusionMath.ApplyWall(Open, Floor, damping: 0.5f), Delta);
        }

        [Test]
        public void ApplyWall_ZeroDamping_LeavesCutoffOpen()
        {
            Assert.AreEqual(Open, WallOcclusionMath.ApplyWall(Open, Floor, damping: 0f), Delta);
        }

        [Test]
        public void ApplyWall_FullDamping_DropsToFloor()
        {
            Assert.AreEqual(Floor, WallOcclusionMath.ApplyWall(Open, Floor, damping: 1f), Delta);
        }

        [Test]
        public void ApplyWall_AccumulatesMultiplicativelyAcrossWalls()
        {
            float afterFirst = WallOcclusionMath.ApplyWall(Open, Floor, 0.5f);
            Assert.AreEqual(6250f, WallOcclusionMath.ApplyWall(afterFirst, Floor, 0.5f), Delta);
        }

        [Test]
        public void ApplyWall_PerWallAbsoluteStepDiminishes()
        {
            float afterFirst = WallOcclusionMath.ApplyWall(Open, Floor, 0.5f);
            float afterSecond = WallOcclusionMath.ApplyWall(afterFirst, Floor, 0.5f);

            float firstStep = Open - afterFirst;
            float secondStep = afterFirst - afterSecond;
            Assert.Less(secondStep, firstStep);
        }

        [Test]
        public void ApplyWall_IsOrderIndependent()
        {
            float lowThenHigh = WallOcclusionMath.ApplyWall(WallOcclusionMath.ApplyWall(Open, Floor, 0.3f), Floor, 0.8f);
            float highThenLow = WallOcclusionMath.ApplyWall(WallOcclusionMath.ApplyWall(Open, Floor, 0.8f), Floor, 0.3f);

            Assert.AreEqual(3940f, lowThenHigh, Delta);
            Assert.AreEqual(lowThenHigh, highThenLow, Delta);
        }

        [Test]
        public void ApplyWall_ThenClampToFloor_OverDampedConfigRescuedToFloor()
        {
            float belowFloor = WallOcclusionMath.ApplyWall(Open, Floor, damping: 1.5f);
            Assert.AreEqual(Floor, WallOcclusionMath.ClampToFloor(belowFloor, Floor), Delta);
        }

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
