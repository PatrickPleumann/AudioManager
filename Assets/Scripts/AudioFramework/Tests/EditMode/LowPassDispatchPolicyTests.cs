using NUnit.Framework;
using AudioFramework.Services.Playback;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the low-pass filter state a freshly dispatched pool slot must carry (W2).
    /// Every expected value is hand-derived from the agreed design, NOT read off the implementation:
    /// non-wall-checked sounds bypass the filter entirely (transparent + no DSP), wall-checked sounds
    /// keep it enabled at the open cutoff so the wall-check loop can ride the cutoff down later.
    /// If the implementation disagrees with these, the implementation is wrong, not the test.
    /// </summary>
    public class LowPassDispatchPolicyTests
    {
        private const float Delta = 1e-5f;

        [Test]
        public void Resolve_NonWallCheckSound_DisablesFilter()
        {
            LowPassDispatchState state = LowPassDispatchPolicy.Resolve(useWallCheck: false, defaultCutoffFrequency: 22000f);
            Assert.IsFalse(state.Enabled, "A sound without wall-check must bypass the low-pass entirely (transparent + no DSP).");
        }

        [Test]
        public void Resolve_WallCheckSound_EnablesFilter()
        {
            LowPassDispatchState state = LowPassDispatchPolicy.Resolve(useWallCheck: true, defaultCutoffFrequency: 22000f);
            Assert.IsTrue(state.Enabled, "A wall-checked sound keeps the filter enabled so the wall-check loop can ride the cutoff down.");
        }

        [Test]
        public void Resolve_WallCheckSound_StartsAtOpenCutoff()
        {
            // Until the first wall-check tick runs, the slot must start transparent (open cutoff), not pre-occluded.
            LowPassDispatchState state = LowPassDispatchPolicy.Resolve(useWallCheck: true, defaultCutoffFrequency: 22000f);
            Assert.AreEqual(22000f, state.CutoffFrequency, Delta);
        }

        [Test]
        public void Resolve_ForwardsConfiguredCutoff_Regardless()
        {
            // The policy must not invent a frequency — it forwards whatever the system config defines as "open".
            LowPassDispatchState state = LowPassDispatchPolicy.Resolve(useWallCheck: false, defaultCutoffFrequency: 17500f);
            Assert.AreEqual(17500f, state.CutoffFrequency, Delta);
        }
    }
}
