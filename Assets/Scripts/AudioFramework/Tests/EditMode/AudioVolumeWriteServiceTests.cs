using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the single volume writer. Model: every sounding slot receives clamp01(base · fade · duck), where
    /// base and duck are looked up by THAT slot's category and fade is the slot's own factor. A silent slot is not
    /// written at all. A missing factor source contributes 1.0, so any factor can be left out without changing the
    /// remaining ones. Every expected value is hand-derived from that spec, NOT read off the implementation. If
    /// the implementation disagrees with these, the implementation is wrong.
    /// </summary>
    public class AudioVolumeWriteServiceTests
    {
        private const float Delta = 1e-5f;

        private static AudioVolumeWriteService ServiceFor(
            IVolumeTarget[] targets,
            ICategoryFactorSource baseSource,
            ICategoryFactorSource duckSource)
        {
            return new AudioVolumeWriteService(targets, baseSource, duckSource);
        }

        [Test]
        public void PlayingSlot_ReceivesProductOfAllFactors()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.Music, FadeFactor = 0.8f };
            var baseSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 0.5f);
            var duckSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 0.5f);

            ServiceFor(new IVolumeTarget[] { slot }, baseSource, duckSource).Apply();

            Assert.AreEqual(0.2f, slot.Volume, Delta);
        }

        [Test]
        public void SilentSlot_IsNotWrittenAtAll()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.Music, IsPlaying = false };
            var baseSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 0.5f);
            var duckSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 1f);

            ServiceFor(new IVolumeTarget[] { slot }, baseSource, duckSource).Apply();

            Assert.AreEqual(0, slot.WriteCount);
        }

        [Test]
        public void MissingDuckSource_ContributesUnity()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.Music, FadeFactor = 0.8f };
            var baseSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 0.5f);

            ServiceFor(new IVolumeTarget[] { slot }, baseSource, null).Apply();

            Assert.AreEqual(0.4f, slot.Volume, Delta);
        }

        [Test]
        public void MissingBaseSource_ContributesUnity()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.Music, FadeFactor = 0.5f };
            var duckSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 0.5f);

            ServiceFor(new IVolumeTarget[] { slot }, null, duckSource).Apply();

            Assert.AreEqual(0.25f, slot.Volume, Delta);
        }

        [Test]
        public void NoSourcesAtAll_WritesFadeFactorOnly()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.Music, FadeFactor = 0.6f };

            ServiceFor(new IVolumeTarget[] { slot }, null, null).Apply();

            Assert.AreEqual(0.6f, slot.Volume, Delta);
        }

        [Test]
        public void ProductAboveUnity_IsClampedToUnity()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.Music, FadeFactor = 1f };
            var baseSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 2f);
            var duckSource = new FakeCategoryFactorSource().With(AudioCategory.Music, 1f);

            ServiceFor(new IVolumeTarget[] { slot }, baseSource, duckSource).Apply();

            Assert.AreEqual(1f, slot.Volume, Delta);
        }

        [Test]
        public void SlotCategoryDecidesTheLookup()
        {
            var slot = new FakeVolumeTarget { Category = AudioCategory.SFX, FadeFactor = 1f };
            var baseSource = new FakeCategoryFactorSource()
                .With(AudioCategory.Music, 0.1f)
                .With(AudioCategory.SFX, 0.9f);
            var duckSource = new FakeCategoryFactorSource()
                .With(AudioCategory.Music, 0.1f)
                .With(AudioCategory.SFX, 1f);

            ServiceFor(new IVolumeTarget[] { slot }, baseSource, duckSource).Apply();

            Assert.AreEqual(0.9f, slot.Volume, Delta);
        }

        [Test]
        public void EachSlotIsResolvedWithItsOwnCategoryAndFade()
        {
            var music = new FakeVolumeTarget { Category = AudioCategory.Music, FadeFactor = 1f };
            var sfx = new FakeVolumeTarget { Category = AudioCategory.SFX, FadeFactor = 0.5f };
            var baseSource = new FakeCategoryFactorSource()
                .With(AudioCategory.Music, 0.4f)
                .With(AudioCategory.SFX, 0.8f);
            var duckSource = new FakeCategoryFactorSource()
                .With(AudioCategory.Music, 0.5f)
                .With(AudioCategory.SFX, 1f);

            ServiceFor(new IVolumeTarget[] { music, sfx }, baseSource, duckSource).Apply();

            Assert.AreEqual(0.2f, music.Volume, Delta);
            Assert.AreEqual(0.4f, sfx.Volume, Delta);
        }

        [Test]
        public void SilentSlotDoesNotStopLaterSlotsFromBeingWritten()
        {
            var silent = new FakeVolumeTarget { Category = AudioCategory.Music, IsPlaying = false };
            var sounding = new FakeVolumeTarget { Category = AudioCategory.SFX, FadeFactor = 1f };
            var baseSource = new FakeCategoryFactorSource()
                .With(AudioCategory.Music, 0.5f)
                .With(AudioCategory.SFX, 0.7f);
            var duckSource = new FakeCategoryFactorSource()
                .With(AudioCategory.Music, 1f)
                .With(AudioCategory.SFX, 1f);

            ServiceFor(new IVolumeTarget[] { silent, sounding }, baseSource, duckSource).Apply();

            Assert.AreEqual(0.7f, sounding.Volume, Delta);
        }
    }
}
