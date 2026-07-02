using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-category duck-factor resolver (step 3 of the mixer/ducking feature). Model: over all
    /// configured pairs targeting the queried category, whose trigger differs from it (a category never ducks
    /// itself) and is currently active, the STRONGEST duck wins — i.e. the minimum DuckedVolume factor. No such
    /// pair → 1.0 (not ducked). Every expected value is hand-derived from that spec, NOT read off the
    /// implementation. If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class DuckTargetPolicyTests
    {
        private const float Delta = 1e-5f;

        [Test]
        public void NoActiveTriggerForTarget_ReturnsUnity()
        {
            var active = new[] { AudioCategory.Player };
            var pairs = new[] { new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.5f) };

            Assert.AreEqual(1.0f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void SingleActiveTrigger_ReturnsItsFactor()
        {
            var active = new[] { AudioCategory.SFX };
            var pairs = new[] { new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.6f) };

            Assert.AreEqual(0.6f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void TwoActiveTriggers_ReturnsMinimum()
        {
            var active = new[] { AudioCategory.SFX, AudioCategory.Player };
            var pairs = new[]
            {
                new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.5f),
                new DuckPair(AudioCategory.Player, AudioCategory.Music, 0.8f)
            };

            Assert.AreEqual(0.5f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void PairTriggerNotActive_ReturnsUnity()
        {
            var active = new AudioCategory[0];
            var pairs = new[] { new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.5f) };

            Assert.AreEqual(1.0f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void SelfDuckSkipped_ReturnsUnity()
        {
            var active = new[] { AudioCategory.Music };
            var pairs = new[] { new DuckPair(AudioCategory.Music, AudioCategory.Music, 0.3f) };

            Assert.AreEqual(1.0f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void PairTargetsOtherCategory_ReturnsUnity()
        {
            var active = new[] { AudioCategory.SFX };
            var pairs = new[] { new DuckPair(AudioCategory.SFX, AudioCategory.Ambient, 0.4f) };

            Assert.AreEqual(1.0f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void EmptyPairs_ReturnsUnity()
        {
            var active = new[] { AudioCategory.SFX };
            var pairs = new DuckPair[0];

            Assert.AreEqual(1.0f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }

        [Test]
        public void ActiveTriggerBeatsSmallerInactive_ReturnsActiveFactor()
        {
            var active = new[] { AudioCategory.SFX };
            var pairs = new[]
            {
                new DuckPair(AudioCategory.SFX, AudioCategory.Music, 0.6f),
                new DuckPair(AudioCategory.Player, AudioCategory.Music, 0.3f)
            };

            Assert.AreEqual(0.6f, DuckTargetPolicy.ResolveDuck(AudioCategory.Music, active, pairs), Delta);
        }
    }
}
