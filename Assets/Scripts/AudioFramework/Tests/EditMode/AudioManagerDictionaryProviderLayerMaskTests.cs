using NUnit.Framework;
using AudioFramework.Core;
using AudioFramework.Utilities;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for building the wall layer-mask lookup (layer index -> wall damping factor) from the configured
    /// WallDampingLayer[]. Every expected value is hand-derived from the agreed contract, NOT read off
    /// the implementation:
    ///   • each entry maps SingleLayer -> WallDampingFactor,
    ///   • a duplicate layer keeps the FIRST value (deterministic config; later duplicates ignored),
    ///   • null / empty input is a silent no-op (the dictionary stays empty, never throws).
    /// If the implementation disagrees with these, the implementation is wrong, not the test.
    /// </summary>
    public class AudioManagerDictionaryProviderLayerMaskTests
    {
        private const float Delta = 1e-5f;

        private static WallDampingLayer Layer(int layer, float value)
            => new WallDampingLayer { SingleLayer = layer, WallDampingFactor = value };

        [Test]
        public void MapsEachLayerToItsCutoff()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillLayerMaskDictionaryWithLayerRelatedValues(new[]
            {
                Layer(8, 0.5f),
                Layer(10, 0.7f)
            });

            Assert.AreEqual(2, provider.WallLayerMaskDictionary.Count);
            Assert.AreEqual(0.5f, provider.WallLayerMaskDictionary[8], Delta);
            Assert.AreEqual(0.7f, provider.WallLayerMaskDictionary[10], Delta);
        }

        [Test]
        public void DuplicateLayer_KeepsFirstValue()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillLayerMaskDictionaryWithLayerRelatedValues(new[]
            {
                Layer(8, 0.5f),
                Layer(8, 0.9f) // same layer, later value must be ignored
            });

            Assert.AreEqual(1, provider.WallLayerMaskDictionary.Count);
            Assert.AreEqual(0.5f, provider.WallLayerMaskDictionary[8], Delta);
        }

        [Test]
        public void NullInput_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillLayerMaskDictionaryWithLayerRelatedValues(null);

            Assert.AreEqual(0, provider.WallLayerMaskDictionary.Count);
        }

        [Test]
        public void EmptyInput_LeavesDictionaryEmpty()
        {
            var provider = new AudioManagerDictionaryProvider();
            provider.FillLayerMaskDictionaryWithLayerRelatedValues(new WallDampingLayer[0]);

            Assert.AreEqual(0, provider.WallLayerMaskDictionary.Count);
        }
    }
}
