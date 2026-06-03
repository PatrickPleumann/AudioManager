using UnityEngine;
using AudioFramework.Services.WallCheck;
using AudioFramework.Data;
using AudioFramework.Core;
using AudioFramework.Pooling;
using AudioFramework.Utilities;
using AudioFramework.Interfaces;

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

        public AudioHandle DispatchAudio(AudioDataObject audioDataObject, Transform source)
        {
            if (source == null)
            {
                string adoName = audioDataObject != null ? audioDataObject.name : "null";
                Debug.LogError($"[AudioTool] PlaySpatial() was called for '{adoName}' without a source Transform. Use PlayNonSpatial() for 2D sounds.");
                return new AudioHandle(-1);
            }
            return Dispatch(audioDataObject, source, isSpatial: true);
        }

        public AudioHandle DispatchAudioNonSpatial(AudioDataObject audioDataObject)
            => Dispatch(audioDataObject, null, isSpatial: false);

        private AudioHandle Dispatch(AudioDataObject audioDataObject, Transform sourceTransform, bool isSpatial)
        {
            if (audioDataObject == null)
            {
                Debug.LogError("[AudioTool] Play() was called with a null AudioDataObject. Skipping playback.");
                return new AudioHandle(-1);
            }

            if (audioDataObject.CurrentClips == null || audioDataObject.CurrentClips.Length == 0)
            {
                Debug.LogError($"[AudioTool] AudioDataObject '{audioDataObject.name}' has no CurrentClips assigned. Skipping playback.");
                return new AudioHandle(-1);
            }

            int poolIndex = poolAcquisitionService.GetFreeAudioSourcePoolIndex();
            if (poolIndex == -1) return new AudioHandle(-1);

            AudioObject poolObject = poolAcquisitionService.PoolArray[poolIndex];
            AudioSource source = poolObject.Source;
            AudioLowPassFilter filter = poolObject.Filter;

            AudioClip currentClip = audioDataObject.CurrentClips[Random.Range(0, audioDataObject.CurrentClips.Length)];
            source.clip = currentClip;

            if (dictionaryProvider.VolumeDictionary.TryGetValue(audioDataObject.CurrentType, out float curVolume))
                source.volume = curVolume;

            // Always written, so a pooled slot never carries the previous sound's spatialization.
            source.spatialBlend = isSpatial ? audioDataObject.SpatialBlend : 0f;

            if (isSpatial)
            {
                if (audioDataObject.SetCallerAsParent) //not cheap in performance
                {
                    poolObject.GameObject.transform.SetParent(sourceTransform);
                    poolObject.GameObject.transform.position = sourceTransform.position;
                }
                else
                    poolObject.GameObject.transform.position = sourceTransform.position;
            }

            filter.cutoffFrequency = defaultCutoffValue;

            if (audioDataObject.IsOneShot)
            {
                poolAcquisitionService.SetSlotBusy(poolIndex, currentClip.length);
                source.PlayOneShot(currentClip);

                if (isSpatial && audioDataObject.UseWallCheck)
                    wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex);

                return audioDataObject.CanHandleAudioSource ? new AudioHandle(poolIndex) : new AudioHandle(-1);
            }
            poolAcquisitionService.ResetSlotBusy(poolIndex);

            source.Play();

            if (isSpatial && audioDataObject.UseWallCheck)
                wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex);
            return audioDataObject.CanHandleAudioSource ? new AudioHandle(poolIndex) : new AudioHandle(-1);
        }

        public void StopAudio(AudioHandle handle)
        {
            if (!handle.IsValid) return;

            int targetIndex = handle.PoolIndex;
            if (poolAcquisitionService.PoolArray[targetIndex].Source != null)
                poolAcquisitionService.PoolArray[targetIndex].Source.Stop();

            poolAcquisitionService.ResetSlotBusy(targetIndex);
            wallCheckService.StopActiveCheck(targetIndex);
        }
    }
}