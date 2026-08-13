using System.Collections.Generic;

using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-category base-gain source. Model: a category that has a configured entry resolves to
    /// exactly that entry, including 0; a category without one resolves to 1.0, so an unconfigured sound plays at
    /// full volume instead of silently disappearing. The value is passed through unclamped — combining and
    /// clamping belong to VolumeResolver — and it is read live from the dictionary, so a settings slider takes
    /// effect immediately. Every expected value is hand-derived from that spec, NOT read off the implementation.
    /// If the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class CategoryVolumeSourceTests
    {
        private const float Delta = 1e-5f;

        [Test]
        public void ConfiguredCategory_ReturnsItsValue()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            Assert.AreEqual(0.6f, new CategoryVolumeSource(volumes).For(AudioCategory.Music), Delta);
        }

        [Test]
        public void UnconfiguredCategory_ReturnsUnity()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            Assert.AreEqual(1.0f, new CategoryVolumeSource(volumes).For(AudioCategory.SFX), Delta);
        }

        [Test]
        public void EmptyDictionary_ReturnsUnity()
        {
            var volumes = new Dictionary<AudioCategory, float>();

            Assert.AreEqual(1.0f, new CategoryVolumeSource(volumes).For(AudioCategory.Music), Delta);
        }

        [Test]
        public void ConfiguredZero_ReturnsZeroNotUnity()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0f } };

            Assert.AreEqual(0f, new CategoryVolumeSource(volumes).For(AudioCategory.Music), Delta);
        }

        [Test]
        public void ValueAboveUnity_IsReturnedUnclamped()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 1.5f } };

            Assert.AreEqual(1.5f, new CategoryVolumeSource(volumes).For(AudioCategory.Music), Delta);
        }

        [Test]
        public void ValueChangedAfterConstruction_IsReadLive()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };
            var source = new CategoryVolumeSource(volumes);

            volumes[AudioCategory.Music] = 0.2f;

            Assert.AreEqual(0.2f, source.For(AudioCategory.Music), Delta);
        }

        [Test]
        public void CategoryAddedAfterConstruction_IsReadLive()
        {
            var volumes = new Dictionary<AudioCategory, float>();
            var source = new CategoryVolumeSource(volumes);

            volumes[AudioCategory.Music] = 0.3f;

            Assert.AreEqual(0.3f, source.For(AudioCategory.Music), Delta);
        }
    }
}
