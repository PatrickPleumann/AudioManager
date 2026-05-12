using System.Collections.Generic;
using UnityEngine;
public class AudioManagerDictionaryProvider
{
    public Dictionary<int, float> WallLayerMaskDictionary = new();
    public Dictionary<AudioTypeProvider, float> volumeDictionary = new();

    /// <summary>
    /// Fills the a Dictionary With LayerMask values as key and sound related "Cutoff Frequency"-float as value.
    /// </summary>
    /// <param name="_cutoffFreqArray"></param>
    public void FillLayerMaskDictionaryWithLayerRelatedValues(CutoffFreqLayerBehaviour[] _cutoffFreqArray)
    {
        if (_cutoffFreqArray != null && _cutoffFreqArray.Length > 0)
        {
            for (int i = 0; i < _cutoffFreqArray.Length; i++)
            {
                WallLayerMaskDictionary.Add(_cutoffFreqArray[i].SingleLayer, _cutoffFreqArray[i].CutoffFrequencyValue);
            }
        }
    }

    /// <summary>
    /// Fills a Dictionary With a pair of AudioTypeProvider & specific Volume.
    /// </summary>
    /// <param name="_transferObject"></param>
    public void FillDictionaryWithKeysAndValues(AudioVolumesTransferObject _transferObject)
    {
        if (_transferObject.AudioVolumes == null)
        {
            Debug.LogWarning("Transfer Object (Array) is null");
            return;
        }
        if (_transferObject.AudioVolumes.Length <= 0)
        {
            Debug.LogWarning("No Audio Volumes found in Transfer Object");
            return;
        }

        if (_transferObject.AudioVolumes != null && _transferObject.AudioVolumes.Length > 0)
            for (int i = 0; i < _transferObject.AudioVolumes.Length; i++)
            {
                if (_transferObject.AudioVolumes[i] != null)
                    volumeDictionary.Add(_transferObject.AudioVolumes[i].CurrentAudioType, _transferObject.AudioVolumes[i].Volume);
                else
                    Debug.Log("Audio Volume Array position: " + i + " is null. Check if your AudioVolumesTransferObject may has a empty spot");
            }
    }
}
