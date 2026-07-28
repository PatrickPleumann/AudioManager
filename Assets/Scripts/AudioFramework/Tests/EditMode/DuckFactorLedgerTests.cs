using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-category duck-factor ledger (step 5 of the mixer/ducking feature). Model: a category is
    /// tracked while it is a configured duck target OR while it is still recovering; each frame every tracked
    /// category glides toward the factor the policy resolves for it, and is retired once it is back at 1.0.
    /// Dropping a rule therefore RELEASES the affected category instead of freezing it at its last ducked value.
    /// When the whole config disappears, the ledger releases with the rates of its most recent step — a config
    /// that is gone can no longer supply them.
    /// Every expected value is hand-derived from that spec, NOT read off the implementation. If the implementation
    /// disagrees with these, the implementation is wrong.
    /// </summary>
    public class DuckFactorLedgerTests
    {
        private const float Delta = 1e-5f;

        [Test]
        public void Step_TargetNoLongerConfigured_GlidesBackTowardUnity()
        {
            var ledger = new DuckFactorLedger();
            var active = new[] { AudioCategory.SFX };
            var configured = new[] { new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.5f) };

            ledger.Step(active, configured, deltaTime: 0.1f, attackRate: 0f, releaseRate: 0.25f);
            ledger.Step(active, new DuckPair[0], deltaTime: 1.0f, attackRate: 0f, releaseRate: 0.25f);

            Assert.AreEqual(0.75f, ledger.FactorFor(AudioCategory.Music), Delta);
        }

        [Test]
        public void ReleaseAll_AfterConfigDisappears_GlidesOutWithLastKnownReleaseRate()
        {
            var ledger = new DuckFactorLedger();
            var active = new[] { AudioCategory.SFX };
            var configured = new[] { new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.5f) };

            ledger.Step(active, configured, deltaTime: 0.1f, attackRate: 0f, releaseRate: 0.4f);
            ledger.ReleaseAll(deltaTime: 0.5f);

            Assert.AreEqual(0.7f, ledger.FactorFor(AudioCategory.Music), Delta);
        }
    }
}
