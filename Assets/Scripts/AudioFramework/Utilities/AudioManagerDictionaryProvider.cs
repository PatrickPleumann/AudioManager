using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Configuration;
using AudioFramework.Core;

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
        /// <param name="_categoryVolumes"></param>
        public void FillDictionaryWithKeysAndValues(IReadOnlyList<CategoryVolume> _categoryVolumes)
        {
            if (_categoryVolumes == null || _categoryVolumes.Count <= 0)
            {
                Debug.LogWarning("[AudioTool] No category volumes configured in the AudioSystemConfig. Every category plays at full volume.");
                return;
            }

            for (int i = 0; i < _categoryVolumes.Count; i++)
            {
                if (!VolumeDictionary.TryAdd(_categoryVolumes[i].Category, _categoryVolumes[i].Volume))
                    Debug.LogWarning($"[AudioTool] Duplicate AudioType '{_categoryVolumes[i].Category}' in the volume list. Keeping the first value, ignoring the duplicate.");
            }
        }
    }
}
