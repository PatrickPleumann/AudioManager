using NUnit.Framework;

using AudioFramework.Configuration;
using AudioFramework.Core;
using AudioFramework.Utilities;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for building the volume lookup (AudioCategory -> volume) from the category volume list authored in
    /// the AudioSystemConfig. Every expected value is hand-derived from the agreed contract, NOT read off the
    /// implementation:
    ///   • each entry maps Category -> Volume,
    ///   • a null list and an empty list are silent no-ops (dictionary stays empty),
    ///   • a duplicate category keeps the FIRST value (keep-first, like the layer-mask map).
    /// If the implementation disagrees with these, the implementation is wrong, not the test.
    /// </summary>
    public class AudioManagerDictionaryProviderVolumeTests
    {
        private const float Delta = 1e-5f;

        private static CategoryVolume Vol(AudioCategory category, float volume)
            => new CategoryVolume { Category = category, Volume = volume };

        [Test]
        public void MapsEachCategoryToItsVolume()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(new[]
            {
                Vol(AudioCategory.Ambient, 0.5f),
                Vol(AudioCategory.Music, 0.8f)
            });

            Assert.AreEqual(2, provider.VolumeDictionary.Count);
            Assert.AreEqual(0.5f, provider.VolumeDictionary[AudioCategory.Ambient], Delta);
            Assert.AreEqual(0.8f, provider.VolumeDictionary[AudioCategory.Music], Delta);
        }

        [Test]
        public void NullVolumeList_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(null);

            Assert.AreEqual(0, provider.VolumeDictionary.Count);
        }

        [Test]
        public void EmptyVolumesArray_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(new CategoryVolume[0]);

            Assert.AreEqual(0, provider.VolumeDictionary.Count);
        }

        [Test]
        public void DuplicateCategory_KeepsFirstValue()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(new[]
            {
                Vol(AudioCategory.SFX, 0.3f),
                Vol(AudioCategory.SFX, 0.9f)
            });

            Assert.AreEqual(1, provider.VolumeDictionary.Count);
            Assert.AreEqual(0.3f, provider.VolumeDictionary[AudioCategory.SFX], Delta);
        }
    }
}
