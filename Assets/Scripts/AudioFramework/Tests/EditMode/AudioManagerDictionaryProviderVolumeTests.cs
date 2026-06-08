using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Data;
using AudioFramework.Utilities;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for building the volume lookup (AudioCategory -> volume) from the AudioVolumesTransferObject.
    /// Every expected value is hand-derived from the agreed contract, NOT read off the implementation:
    ///   • each entry maps CurrentAudioType -> Volume,
    ///   • null transfer object / null array / empty array are silent no-ops (dictionary stays empty),
    ///   • a null entry inside the array is skipped but iteration CONTINUES (a valid entry after it still maps),
    ///   • a duplicate category keeps the FIRST value (keep-first, like the layer-mask map).
    /// If the implementation disagrees with these, the implementation is wrong, not the test.
    ///
    /// These tests build real UnityEngine.Objects (ScriptableObjects), so each created instance is tracked and
    /// destroyed in TearDown — DestroyImmediate (not Destroy) because Destroy is deferred and would not run in
    /// EditMode tests, leaking the native object.
    /// </summary>
    public class AudioManagerDictionaryProviderVolumeTests
    {
        private const float Delta = 1e-5f;

        private readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        private AudioSourceVolumes Vol(AudioCategory category, float volume)
        {
            var so = ScriptableObject.CreateInstance<AudioSourceVolumes>();
            so.CurrentAudioType = category;
            so.Volume = volume;
            created.Add(so);
            return so;
        }

        private AudioVolumesTransferObject Transfer(params AudioSourceVolumes[] volumes)
        {
            var transfer = ScriptableObject.CreateInstance<AudioVolumesTransferObject>();
            transfer.AudioVolumes = volumes;
            created.Add(transfer);
            return transfer;
        }

        [Test]
        public void MapsEachCategoryToItsVolume()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(Transfer(
                Vol(AudioCategory.Ambient, 0.5f),
                Vol(AudioCategory.Music, 0.8f)));

            Assert.AreEqual(2, provider.VolumeDictionary.Count);
            Assert.AreEqual(0.5f, provider.VolumeDictionary[AudioCategory.Ambient], Delta);
            Assert.AreEqual(0.8f, provider.VolumeDictionary[AudioCategory.Music], Delta);
        }

        [Test]
        public void NullTransferObject_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(null);

            Assert.AreEqual(0, provider.VolumeDictionary.Count);
        }

        [Test]
        public void NullVolumesArray_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            var transfer = ScriptableObject.CreateInstance<AudioVolumesTransferObject>();
            transfer.AudioVolumes = null;
            created.Add(transfer);

            provider.FillDictionaryWithKeysAndValues(transfer);

            Assert.AreEqual(0, provider.VolumeDictionary.Count);
        }

        [Test]
        public void EmptyVolumesArray_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(Transfer()); // params -> empty array

            Assert.AreEqual(0, provider.VolumeDictionary.Count);
        }

        [Test]
        public void NullEntry_IsSkipped_RestStillMapped()
        {
            var provider = new AudioManagerDictionaryProvider();
            // A null slot precedes a valid entry: the null is skipped, iteration continues, the valid one still maps.
            provider.FillDictionaryWithKeysAndValues(Transfer(
                null,
                Vol(AudioCategory.Music, 0.8f)));

            Assert.AreEqual(1, provider.VolumeDictionary.Count);
            Assert.AreEqual(0.8f, provider.VolumeDictionary[AudioCategory.Music], Delta);
        }

        [Test]
        public void DuplicateCategory_KeepsFirstValue()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillDictionaryWithKeysAndValues(Transfer(
                Vol(AudioCategory.SFX, 0.3f),
                Vol(AudioCategory.SFX, 0.9f))); // same category, later value must be ignored

            Assert.AreEqual(1, provider.VolumeDictionary.Count);
            Assert.AreEqual(0.3f, provider.VolumeDictionary[AudioCategory.SFX], Delta);
        }
    }
}
