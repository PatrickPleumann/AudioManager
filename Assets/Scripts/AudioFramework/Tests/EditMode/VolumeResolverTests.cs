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

        [Test]
        public void Resolve_AllNeutral_ReturnsOne()
        {
            Assert.AreEqual(1f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_CategoryVolumeOnly_PassesThrough()
        {
            Assert.AreEqual(0.6f, VolumeResolver.Resolve(basis: 0.6f, fade: 1f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_FadeFactorOnly_Scales()
        {
            Assert.AreEqual(0.5f, VolumeResolver.Resolve(basis: 1f, fade: 0.5f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_DuckFactorOnly_Scales()
        {
            Assert.AreEqual(0.5f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: 0.5f), Delta);
        }

        [Test]
        public void Resolve_AllThree_Multiply()
        {
            Assert.AreEqual(0.2f, VolumeResolver.Resolve(basis: 0.8f, fade: 0.5f, duck: 0.5f), Delta);
        }

        [Test]
        public void Resolve_DuckToZero_Silences()
        {
            Assert.AreEqual(0f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: 0f), Delta);
        }

        [Test]
        public void Resolve_ProductAboveOne_ClampsToOne()
        {
            Assert.AreEqual(1f, VolumeResolver.Resolve(basis: 2f, fade: 1f, duck: 1f), Delta);
        }

        [Test]
        public void Resolve_NegativeFactor_ClampsToZero()
        {
            Assert.AreEqual(0f, VolumeResolver.Resolve(basis: 1f, fade: 1f, duck: -0.5f), Delta);
        }
    }
}
