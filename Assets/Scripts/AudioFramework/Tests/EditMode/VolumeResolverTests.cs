using NUnit.Framework;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the stage-1 gain resolver: <c>source.volume = clamp01(basis · fade · duck)</c>.
    /// Every expected value below is hand-derived from the agreed spec (BACKLOG: "Volume-Gleichung &amp;
    /// Resolver"), NOT read off the implementation. If the implementation disagrees with these, the
    /// implementation is wrong, not the test.
    /// </summary>
    public class VolumeResolverTests
    {
        private const float Delta = 1e-5f;

        // --- The three factors multiply; each is neutral at 1.0 ---

        [Test]
        public void Resolve_AllNeutral_ReturnsOne()
        {
            // 1 · 1 · 1 = 1 (full volume, nothing scaling it down).
            Assert.AreEqual(1f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_CategoryVolumeOnly_PassesThrough()
        {
            // Settings slider at 0.6, no fade, no duck => 0.6.
            Assert.AreEqual(0.6f, VolumeResolver.Resolve(basis: 0.6f, fade: 1f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_FadeFactorOnly_Scales()
        {
            // Mid-fade factor 0.5 against full base => 0.5.
            Assert.AreEqual(0.5f, VolumeResolver.Resolve(basis: 1f, fade: 0.5f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_DuckFactorOnly_Scales()
        {
            // Ducked to half against full base => 0.5.
            Assert.AreEqual(0.5f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: 0.5f), Delta);
        }

        [Test]
        public void Resolve_AllThree_Multiply()
        {
            // 0.8 · 0.5 · 0.5 = 0.2 — proves all three stages compose multiplicatively.
            Assert.AreEqual(0.2f, VolumeResolver.Resolve(basis: 0.8f, fade: 0.5f, duck: 0.5f), Delta);
        }

        [Test]
        public void Resolve_DuckToZero_Silences()
        {
            // A full duck (0) silences the source regardless of base/fade.
            Assert.AreEqual(0f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: 0f), Delta);
        }

        // --- Defensive clamp to [0, 1] guards against misconfiguration ---

        [Test]
        public void Resolve_ProductAboveOne_ClampsToOne()
        {
            // A misconfigured base > 1 must not push source.volume past 1 (Unity caps it anyway; we own the clamp).
            Assert.AreEqual(1f, VolumeResolver.Resolve(basis: 2f, fade: 1f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_NegativeFactor_ClampsToZero()
        {
            // A negative (misconfigured) factor must not produce a negative volume.
            Assert.AreEqual(0f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: -0.5f), Delta);
        }
    }
}
