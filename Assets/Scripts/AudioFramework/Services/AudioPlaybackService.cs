using UnityEngine;
using AudioFramework.Services.WallCheck;
using AudioFramework.Data;
using AudioFramework.Core;
using AudioFramework.Pooling;
using AudioFramework.Utilities;

namespace AudioFramework.Services.Playback
{
    public class AudioPlaybackService
    {
        private readonly AudioPoolAcquisitionService poolAcquisitionService;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;
        private readonly IAudioWallCheckService wallCheckService;
        private readonly float defaultCutoffValue;

        public AudioPlaybackService(
            AudioPoolAcquisitionService _poolAcquisitionService,
            AudioManagerDictionaryProvider _dictionaryProvider,
            IAudioWallCheckService _wallCheckService,
            float _defaultCutoffValue)
        {
            poolAcquisitionService = _poolAcquisitionService;
            dictionaryProvider = _dictionaryProvider;
            wallCheckService = _wallCheckService;
            defaultCutoffValue = _defaultCutoffValue;
        }

        public AudioHandle DispatchAudio(AudioDataObject audioDataObject)
        {
            int poolIndex = poolAcquisitionService.GetFreePoolIndex();
            if (poolIndex == -1) return new AudioHandle(-1);

            AudioObject poolObject = poolAcquisitionService.PoolArray[poolIndex];
            AudioSource source = poolObject.Source;
            AudioLowPassFilter filter = poolObject.Filter;

            AudioClip chosenClip = audioDataObject.CurrentClips[Random.Range(0, audioDataObject.CurrentClips.Length)];
            source.clip = chosenClip;

            if (dictionaryProvider.VolumeDictionary.TryGetValue(audioDataObject.CurrentType, out float curVolume))
                source.volume = curVolume;

            if (audioDataObject.SetCallerAsParent)
            {
                poolObject.GameObject.transform.SetParent(audioDataObject.CallerTransform);
                poolObject.GameObject.transform.position = audioDataObject.CallerTransform.position;
            }
            else
                poolObject.GameObject.transform.position = audioDataObject.CallerTransform.position;

            filter.cutoffFrequency = defaultCutoffValue;

            if (audioDataObject.IsOneShot)
            {
                poolAcquisitionService.SetSlotBusy(poolIndex, chosenClip.length);
                source.PlayOneShot(chosenClip);

                if (audioDataObject.UseWallCheck)
                    wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex, chosenClip.length);

                return new AudioHandle(-1);
            }
            poolAcquisitionService.ResetSlotBusy(poolIndex);

            if (audioDataObject.UseWallCheck)
                wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex, chosenClip.length);

            source.Play();
            return new AudioHandle(poolIndex);
        }

        public void StopAudio(AudioHandle handle)
        {
            int targetIndex = handle.PoolIndex;
            if (poolAcquisitionService.PoolArray[targetIndex].Source != null)
                poolAcquisitionService.PoolArray[targetIndex].Source.Stop();

            poolAcquisitionService.ResetSlotBusy(targetIndex);
            wallCheckService.StopActiveCheck(targetIndex);
        }
    }
}