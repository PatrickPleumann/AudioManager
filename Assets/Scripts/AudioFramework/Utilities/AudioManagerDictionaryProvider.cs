using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Data;

namespace AudioFramework.Utilities
{
    public class AudioManagerDictionaryProvider
    {
        public readonly Dictionary<int, float> WallLayerMaskDictionary = new();
        public readonly Dictionary<AudioTypeProvider, float> VolumeDictionary = new();

        /// <summary>
        /// Fills the Dictionary With LayerMask values as key and sound related "Cutoff Frequency"-floats as value.
        /// </summary>
        /// <param name="_cutoffFreqArray"></param>
        public void FillLayerMaskDictionaryWithLayerRelatedValues(CutoffFreqLayerBehaviour[] _cutoffFreqArray)
        {
            if (_cutoffFreqArray != null && _cutoffFreqArray.Length > 0)
            {
                for (int i = 0; i < _cutoffFreqArray.Length; i++)
                {
                    if (!WallLayerMaskDictionary.TryAdd(_cutoffFreqArray[i].SingleLayer, _cutoffFreqArray[i].CutoffFrequencyValue))
                        Debug.LogWarning($"[AudioTool] Duplicate layer (index {_cutoffFreqArray[i].SingleLayer}) in CutOffFrequenciesPerLayer. Keeping the first value, ignoring the duplicate.");
                }
            }
        }
        /// <summary>
        /// Fills a Dictionary With a pair of AudioTypeProvider & specific Volume.
        /// </summary>
        /// <param name="_transferObject"></param>

        public void FillDictionaryWithKeysAndValues(AudioVolumesTransferObject _transferObject)
        {
            if (_transferObject == null)
            {
                Debug.LogWarning("[AudioTool] No AudioVolumesTransferObject assigned in the AudioSystemConfig.");
                return;
            }
            if (_transferObject.AudioVolumes == null)
            {
                Debug.LogWarning("[AudioTool] AudioVolumes array is null.");
                return;
            }
            if (_transferObject.AudioVolumes.Length <= 0)
            {
                Debug.LogWarning("[AudioTool] No AudioVolumes found in the Transfer Object.");
                return;
            }

            for (int i = 0; i < _transferObject.AudioVolumes.Length; i++)
            {
                if (_transferObject.AudioVolumes[i] == null)
                {
                    Debug.LogWarning($"[AudioTool] AudioVolumes[{i}] is null. Check your AudioVolumesTransferObject for an empty spot.");
                    continue;
                }

                if (!VolumeDictionary.TryAdd(_transferObject.AudioVolumes[i].CurrentAudioType, _transferObject.AudioVolumes[i].Volume))
                    Debug.LogWarning($"[AudioTool] Duplicate AudioType '{_transferObject.AudioVolumes[i].CurrentAudioType}' in the volume list. Keeping the first value, ignoring the duplicate.");
            }
        }
    }
}
