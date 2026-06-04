using NUnit.Framework;
using AudioFramework.Services.WallCheck;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-frame occlusion cutoff glide (P2 — kills the hard "pop" when a sound moves in/out
    /// of occlusion). The model is MoveTowards: the cutoff travels toward its target at a constant rate of
    /// <c>speed</c> Hz per second, frame-rate independent via deltaTime. Every expected value is hand-derived
    /// from that spec, NOT read off the implementation. If the implementation disagrees, it is wrong.
    /// </summary>
    public class OcclusionSmoothingTests
    {
        private const float Delta = 1e-5f;

        // --- Glides toward the target by speed * deltaTime each step ---

        [Test]
        public void Step_MovesUpTowardTarget_BySpeedTimesDeltaTime()
        {
            // maxStep = 2000 * 0.1 = 200; target far above -> current + 200.
            Assert.AreEqual(300f, OcclusionSmoothing.Step(current: 100f, target: 1000f, deltaTime: 0.1f, speed: 2000f), Delta);
        }

        [Test]
        public void Step_MovesDownTowardTarget_BySpeedTimesDeltaTime()
        {
            // Descending glide (stepping behind a wall): 1000 -> 800 with maxStep 200.
            Assert.AreEqual(800f, OcclusionSmoothing.Step(current: 1000f, target: 100f, deltaTime: 0.1f, speed: 2000f), Delta);
        }

        // --- Settles exactly on the target when within one step (no overshoot / no jitter) ---

        [Test]
        public void Step_WithinOneStepOfTarget_SettlesExactly()
        {
            // diff = 100 <= maxStep 200 -> snap to target, never overshoot.
            Assert.AreEqual(1000f, OcclusionSmoothing.Step(current: 900f, target: 1000f, deltaTime: 0.1f, speed: 2000f), Delta);
        }

        [Test]
        public void Step_AlreadyAtTarget_StaysPut()
        {
            Assert.AreEqual(500f, OcclusionSmoothing.Step(current: 500f, target: 500f, deltaTime: 0.1f, speed: 2000f), Delta);
        }

        // --- Edge cases: smoothing disabled, and a zero-length frame ---

        [Test]
        public void Step_ZeroOrNegativeSpeed_SnapsToTargetInstantly()
        {
            // speed <= 0 disables smoothing -> restores the old hard-set behaviour.
            Assert.AreEqual(1000f, OcclusionSmoothing.Step(current: 100f, target: 1000f, deltaTime: 0.1f, speed: 0f), Delta);
        }

        [Test]
        public void Step_ZeroDeltaTime_DoesNotMove()
        {
            // No time elapsed this frame -> the cutoff must not jump.
            Assert.AreEqual(100f, OcclusionSmoothing.Step(current: 100f, target: 1000f, deltaTime: 0f, speed: 2000f), Delta);
        }
    }
}
