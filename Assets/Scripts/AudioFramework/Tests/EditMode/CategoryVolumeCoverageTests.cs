using System.Collections.Generic;
using NUnit.Framework;

using AudioFramework.Configuration;
using AudioFramework.Core;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the coverage verdict a config gives about its own category volume list. Every expected value
    /// is hand-derived from the agreed contract, NOT read off the implementation:
    ///   • a defined category with no entry is MISSING, reported in enum order,
    ///   • a null list behaves exactly like an empty one (the runtime treats it that way too),
    ///   • a category listed more than once is DUPLICATED exactly once, and counts as configured,
    ///   • an entry at volume 0 is SILENT,
    ///   • for a duplicated category only the FIRST entry decides silence — the later one is dead at runtime
    ///     because the volume dictionary keeps the first value.
    /// If the implementation disagrees with these, the implementation is wrong, not the test.
    /// </summary>
    public class CategoryVolumeCoverageTests
    {
        private static readonly IReadOnlyList<AudioCategory> AllCategories = new[]
        {
            AudioCategory.Ambient,
            AudioCategory.Music,
            AudioCategory.SFX,
            AudioCategory.Player,
            AudioCategory.BehindWall
        };

        private static CategoryVolume Vol(AudioCategory category, float volume)
            => new CategoryVolume { Category = category, Volume = volume };

        private static CategoryVolumeCoverage.Result Evaluate(params CategoryVolume[] configured)
            => CategoryVolumeCoverage.Evaluate(configured, AllCategories);

        [Test]
        public void EveryCategoryConfigured_ReportsNothing()
        {
            CategoryVolumeCoverage.Result result = Evaluate(
                Vol(AudioCategory.Ambient, 1f),
                Vol(AudioCategory.Music, 0.8f),
                Vol(AudioCategory.SFX, 0.5f),
                Vol(AudioCategory.Player, 0.1f),
                Vol(AudioCategory.BehindWall, 0.5f));

            Assert.AreEqual(0, result.Missing.Count);
            Assert.AreEqual(0, result.Duplicated.Count);
            Assert.AreEqual(0, result.Silent.Count);
        }

        [Test]
        public void UnlistedCategories_AreMissingInEnumOrder()
        {
            CategoryVolumeCoverage.Result result = Evaluate(
                Vol(AudioCategory.SFX, 0.5f),
                Vol(AudioCategory.Ambient, 1f));

            Assert.AreEqual(3, result.Missing.Count);
            Assert.AreEqual(AudioCategory.Music, result.Missing[0]);
            Assert.AreEqual(AudioCategory.Player, result.Missing[1]);
            Assert.AreEqual(AudioCategory.BehindWall, result.Missing[2]);
        }

        [Test]
        public void EmptyList_ReportsEveryCategoryMissing()
        {
            CategoryVolumeCoverage.Result result = Evaluate();

            Assert.AreEqual(5, result.Missing.Count);
        }

        [Test]
        public void NullList_ReportsEveryCategoryMissing()
        {
            CategoryVolumeCoverage.Result result = CategoryVolumeCoverage.Evaluate(null, AllCategories);

            Assert.AreEqual(5, result.Missing.Count);
        }

        [Test]
        public void RepeatedCategory_IsDuplicatedAndCountsAsConfigured()
        {
            CategoryVolumeCoverage.Result result = Evaluate(
                Vol(AudioCategory.SFX, 0.5f),
                Vol(AudioCategory.SFX, 0.9f));

            Assert.AreEqual(1, result.Duplicated.Count);
            Assert.AreEqual(AudioCategory.SFX, result.Duplicated[0]);
            CollectionAssert.DoesNotContain(result.Missing, AudioCategory.SFX);
        }

        [Test]
        public void CategoryListedThreeTimes_IsReportedOnce()
        {
            CategoryVolumeCoverage.Result result = Evaluate(
                Vol(AudioCategory.Music, 0.5f),
                Vol(AudioCategory.Music, 0.6f),
                Vol(AudioCategory.Music, 0.7f));

            Assert.AreEqual(1, result.Duplicated.Count);
        }

        [Test]
        public void ZeroVolumeEntry_IsSilent()
        {
            CategoryVolumeCoverage.Result result = Evaluate(
                Vol(AudioCategory.Music, 0f),
                Vol(AudioCategory.SFX, 0.5f));

            Assert.AreEqual(1, result.Silent.Count);
            Assert.AreEqual(AudioCategory.Music, result.Silent[0]);
        }

        [Test]
        public void DuplicateWithZeroAfterAudibleFirst_IsNotSilent()
        {
            CategoryVolumeCoverage.Result result = Evaluate(
                Vol(AudioCategory.SFX, 0.5f),
                Vol(AudioCategory.SFX, 0f));

            Assert.AreEqual(0, result.Silent.Count);
        }
    }
}
