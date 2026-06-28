using NUnit.Framework;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-frame duck-factor glide (step 2 of the mixer/ducking feature). The model is MoveTowards
    /// with an ASYMMETRIC rate: the duck factor (1 = not ducked, 0 = fully silenced) travels toward its target
    /// at <c>attackRate</c> when ducking deeper (factor falling) and <c>releaseRate</c> when recovering (factor
    /// rising), frame-rate independent via deltaTime. Every expected value is hand-derived from that spec, NOT
    /// read off the implementation. If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class DuckEnvelopeTests
    {
        private const float Delta = 1e-5f;

        // --- Direction selects the rate: falling uses attack, rising uses release ---

        [Test]
        public void Step_DuckingDeeper_UsesAttackRate()
        {
            // Factor falling 1.0 -> 0.2: attack rate 4.0 picked. maxStep = 4.0 * 0.1 = 0.4; |diff| 0.8 > 0.4
            // -> 1.0 - 0.4 = 0.6. (If it wrongly used release 1.0 -> maxStep 0.1 -> 0.9.)
            Assert.AreEqual(0.6f, DuckEnvelope.Step(current: 1.0f, target: 0.2f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_Recovering_UsesReleaseRate()
        {
            // Factor rising 0.2 -> 1.0: release rate 1.0 picked. maxStep = 1.0 * 0.1 = 0.1; |diff| 0.8 > 0.1
            // -> 0.2 + 0.1 = 0.3. (If it wrongly used attack 4.0 -> maxStep 0.4 -> 0.6.)
            Assert.AreEqual(0.3f, DuckEnvelope.Step(current: 0.2f, target: 1.0f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        // --- Settles exactly on the target when within one step (no overshoot), both directions ---

        [Test]
        public void Step_WithinOneStepDeeper_SettlesExactly()
        {
            // diff = -0.1, attack maxStep 0.4 -> |diff| <= maxStep -> snap to target, never overshoot.
            Assert.AreEqual(0.4f, DuckEnvelope.Step(current: 0.5f, target: 0.4f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_WithinOneStepRecovering_SettlesExactly()
        {
            // diff = +0.05, release maxStep 0.1 -> |diff| <= maxStep -> snap to target, never overshoot.
            Assert.AreEqual(0.55f, DuckEnvelope.Step(current: 0.5f, target: 0.55f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_AlreadyAtTarget_StaysPut()
        {
            // diff = 0 -> no movement regardless of which rate is selected.
            Assert.AreEqual(0.5f, DuckEnvelope.Step(current: 0.5f, target: 0.5f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        // --- A non-positive selected rate disables smoothing for that direction (instant snap) ---

        [Test]
        public void Step_ZeroAttackRate_SnapsToTargetInstantly()
        {
            // Ducking deeper with attack 0 -> instant snap to 0.2. (A wrong release 1.0 path would give 0.9.)
            Assert.AreEqual(0.2f, DuckEnvelope.Step(current: 1.0f, target: 0.2f, deltaTime: 0.1f, attackRate: 0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_ZeroReleaseRate_SnapsToTargetInstantly()
        {
            // Recovering with release 0 -> instant snap to 1.0. (A wrong attack 4.0 path would give 0.6.)
            Assert.AreEqual(1.0f, DuckEnvelope.Step(current: 0.2f, target: 1.0f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 0f), Delta);
        }

        // --- A zero-length frame produces no movement (positive rate, no time elapsed) ---

        [Test]
        public void Step_ZeroDeltaTime_DoesNotMove()
        {
            // maxStep = 4.0 * 0 = 0 -> the factor must not jump; stays at current.
            Assert.AreEqual(1.0f, DuckEnvelope.Step(current: 1.0f, target: 0.2f, deltaTime: 0f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }
    }
}
