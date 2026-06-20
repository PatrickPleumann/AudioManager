using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Data;

namespace AudioFramework.Utilities
{
    public class AudioManagerDictionaryProvider
    {
        public readonly Dictionary<int, float> WallLayerMaskDictionary = new();
        public readonly Dictionary<AudioCategory, float> VolumeDictionary = new();

        /// <summary>
        /// Fills the Dictionary with the layer index as key and that layer's wall damping factor (0..1) as value.
        /// </summary>
        /// <param name="_wallDampingArray"></param>
        public void FillLayerMaskDictionaryWithLayerRelatedValues(WallDampingLayer[] _wallDampingArray)
        {
            if (_wallDampingArray != null && _wallDampingArray.Length > 0)
            {
                for (int i = 0; i < _wallDampingArray.Length; i++)
                {
                    if (!WallLayerMaskDictionary.TryAdd(_wallDampingArray[i].SingleLayer, _wallDampingArray[i].WallDampingFactor))
                        Debug.LogWarning($"[AudioTool] Duplicate layer (index {_wallDampingArray[i].SingleLayer}) in WallDampingPerLayer. Keeping the first value, ignoring the duplicate.");
                }
            }
        }
        /// <summary>
        /// Fills a Dictionary With a pair of AudioCategory & specific Volume.
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
