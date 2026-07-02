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

        [Test]
        public void Step_DuckingDeeper_UsesAttackRate()
        {
            Assert.AreEqual(0.6f, DuckEnvelope.Step(current: 1.0f, target: 0.2f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_Recovering_UsesReleaseRate()
        {
            Assert.AreEqual(0.3f, DuckEnvelope.Step(current: 0.2f, target: 1.0f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_WithinOneStepDeeper_SettlesExactly()
        {
            Assert.AreEqual(0.4f, DuckEnvelope.Step(current: 0.5f, target: 0.4f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_WithinOneStepRecovering_SettlesExactly()
        {
            Assert.AreEqual(0.55f, DuckEnvelope.Step(current: 0.5f, target: 0.55f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_AlreadyAtTarget_StaysPut()
        {
            Assert.AreEqual(0.5f, DuckEnvelope.Step(current: 0.5f, target: 0.5f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }


        [Test]
        public void Step_ZeroAttackRate_SnapsToTargetInstantly()
        {
            Assert.AreEqual(0.2f, DuckEnvelope.Step(current: 1.0f, target: 0.2f, deltaTime: 0.1f, attackRate: 0f, releaseRate: 1.0f), Delta);
        }

        [Test]
        public void Step_ZeroReleaseRate_SnapsToTargetInstantly()
        {
            Assert.AreEqual(1.0f, DuckEnvelope.Step(current: 0.2f, target: 1.0f, deltaTime: 0.1f, attackRate: 4.0f, releaseRate: 0f), Delta);
        }

        [Test]
        public void Step_ZeroDeltaTime_DoesNotMove()
        {
            Assert.AreEqual(1.0f, DuckEnvelope.Step(current: 1.0f, target: 0.2f, deltaTime: 0f, attackRate: 4.0f, releaseRate: 1.0f), Delta);
        }
    }
}
