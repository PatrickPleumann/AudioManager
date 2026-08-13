using System.Collections.Generic;

using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the per-category base-gain writer — the settings-slider end of the volume dictionary. Model: a
    /// write stores exactly the requested value, clamped to [0, 1] at this boundary so a slider always reads back
    /// what the player actually hears. A category that has no entry yet is created rather than rejected, and the
    /// creation is reported to the caller; an existing entry is overwritten and reported as an update. The
    /// boundary values 0 and 1 are legal and pass through untouched. Every expected value is hand-derived from
    /// that spec, NOT read off the implementation. If the implementation disagrees with these, the implementation
    /// is wrong.
    /// </summary>
    public class CategoryVolumeWriterTests
    {
        private const float Delta = 1e-5f;

        [Test]
        public void ConfiguredCategory_StoresTheRequestedValue()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            new CategoryVolumeWriter(volumes).Set(AudioCategory.Music, 0.3f);

            Assert.AreEqual(0.3f, volumes[AudioCategory.Music], Delta);
        }

        [Test]
        public void ConfiguredCategory_ReportsUpdated()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            Assert.AreEqual(
                CategoryVolumeWriteOutcome.Updated,
                new CategoryVolumeWriter(volumes).Set(AudioCategory.Music, 0.3f));
        }

        [Test]
        public void UnconfiguredCategory_IsCreatedWithTheRequestedValue()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            new CategoryVolumeWriter(volumes).Set(AudioCategory.SFX, 0.4f);

            Assert.AreEqual(0.4f, volumes[AudioCategory.SFX], Delta);
        }

        [Test]
        public void UnconfiguredCategory_ReportsEntryCreated()
        {
            var volumes = new Dictionary<AudioCategory, float>();

            Assert.AreEqual(
                CategoryVolumeWriteOutcome.EntryCreated,
                new CategoryVolumeWriter(volumes).Set(AudioCategory.Music, 0.4f));
        }

        [Test]
        public void ValueAboveUnity_IsClampedToUnity()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            new CategoryVolumeWriter(volumes).Set(AudioCategory.Music, 1.5f);

            Assert.AreEqual(1.0f, volumes[AudioCategory.Music], Delta);
        }

        [Test]
        public void ValueBelowZero_IsClampedToZero()
        {
            var volumes = new Dictionary<AudioCategory, float> { { AudioCategory.Music, 0.6f } };

            new CategoryVolumeWriter(volumes).Set(AudioCategory.Music, -0.5f);

            Assert.AreEqual(0f, volumes[AudioCategory.Music], Delta);
        }

        [Test]
        public void BoundaryValues_ArePassedThroughUnchanged()
        {
            var volumes = new Dictionary<AudioCategory, float>();
            var writer = new CategoryVolumeWriter(volumes);

            writer.Set(AudioCategory.Music, 0f);
            writer.Set(AudioCategory.SFX, 1f);

            Assert.AreEqual(0f, volumes[AudioCategory.Music], Delta);
            Assert.AreEqual(1f, volumes[AudioCategory.SFX], Delta);
        }
    }
}
