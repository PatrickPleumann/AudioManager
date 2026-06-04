using UnityEngine;
using AudioFramework.Services.WallCheck;
using AudioFramework.Services.Fading;
using AudioFramework.Data;
using AudioFramework.Core;
using AudioFramework.Pooling;
using AudioFramework.Utilities;
using AudioFramework.Interfaces;
using AudioFramework.Pause;

namespace AudioFramework.Services.Playback
{
    public class AudioPlaybackService
    {
        private readonly AudioPoolAcquisitionService poolAcquisitionService;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;
        private readonly IAudioWallCheckService wallCheckService;
        private readonly AudioPauseService pauseService;
        private readonly AudioStopService stopService;
        private readonly AudioFadeService fadeService;
        private readonly float defaultCutoffValue;

        public AudioPlaybackService(
            AudioPoolAcquisitionService _poolAcquisitionService,
            AudioManagerDictionaryProvider _dictionaryProvider,
            IAudioWallCheckService _wallCheckService,
            AudioPauseService _pauseService,
            AudioStopService _stopService,
            AudioFadeService _fadeService,
            float _defaultCutoffValue)
        {
            poolAcquisitionService = _poolAcquisitionService;
            dictionaryProvider = _dictionaryProvider;
            wallCheckService = _wallCheckService;
            pauseService = _pauseService;
            stopService = _stopService;
            fadeService = _fadeService;
            defaultCutoffValue = _defaultCutoffValue;
        }

        public AudioHandle DispatchAudio(AudioDataObject audioDataObject, Transform source)
        {
            if (source == null)
            {
                string adoName = audioDataObject != null ? audioDataObject.name : "null";
                Debug.LogError($"[AudioTool] PlaySpatial() was called for '{adoName}' without a source Transform. Use PlayNonSpatial() for 2D sounds.");
                return AudioHandle.Invalid;
            }
            int poolIndex = Dispatch(audioDataObject, source, isSpatial: true, startSilent: false, out _);
            return Gate(poolIndex, audioDataObject);
        }

        public AudioHandle DispatchAudioNonSpatial(AudioDataObject audioDataObject)
        {
            int poolIndex = Dispatch(audioDataObject, null, isSpatial: false, startSilent: false, out _);
            return Gate(poolIndex, audioDataObject);
        }

        /// <summary>
        /// Dispatch a NON-spatial sound that starts SILENT (volume 0) for a fade-in, reporting the category volume the
        /// fade should ramp up to via <paramref name="targetVolume"/>. Returns the raw pool index (-1 if no free slot
        /// or misconfigured) — a fade always tracks its own slot, so this is intentionally NOT gated by
        /// CanHandleAudioSource. The manager registers the fade on the returned index.
        /// </summary>
        public int DispatchSilentNonSpatial(AudioDataObject audioDataObject, out float targetVolume)
            => Dispatch(audioDataObject, null, isSpatial: false, startSilent: true, out targetVolume);

        /// <summary>
        /// Spatial counterpart of <see cref="DispatchSilentNonSpatial"/>: dispatch a positional 3D sound that starts
        /// SILENT for a fade-in at <paramref name="source"/>, reporting the category volume to ramp up to. Returns the
        /// raw pool index (-1 if no slot, misconfigured, or null source) — not gated by CanHandleAudioSource.
        /// </summary>
        public int DispatchSilentSpatial(AudioDataObject audioDataObject, Transform source, out float targetVolume)
        {
            targetVolume = 0f;
            if (source == null)
            {
                string adoName = audioDataObject != null ? audioDataObject.name : "null";
                Debug.LogError($"[AudioTool] FadeInSpatial() was called for '{adoName}' without a source Transform. Use FadeInNonSpatial() for 2D sounds.");
                return -1;
            }
            return Dispatch(audioDataObject, source, isSpatial: true, startSilent: true, out targetVolume);
        }

        private int Dispatch(AudioDataObject audioDataObject, Transform sourceTransform, bool isSpatial, bool startSilent, out float resolvedVolume)
        {
            resolvedVolume = 0f;

            if (audioDataObject == null)
            {
                Debug.LogError("[AudioTool] Play() was called with a null AudioDataObject. Skipping playback.");
                return -1;
            }

            if (audioDataObject.CurrentClips == null || audioDataObject.CurrentClips.Length == 0)
            {
                Debug.LogError($"[AudioTool] AudioDataObject '{audioDataObject.name}' has no CurrentClips assigned. Skipping playback.");
                return -1;
            }

            int poolIndex = poolAcquisitionService.GetFreeAudioSourcePoolIndex();
            if (poolIndex == -1) return -1;

            fadeService.ClearFade(poolIndex);

            AudioObject poolObject = poolAcquisitionService.PoolArray[poolIndex];
            AudioSource source = poolObject.Source;
            AudioLowPassFilter filter = poolObject.Filter;

            AudioClip currentClip = audioDataObject.CurrentClips[Random.Range(0, audioDataObject.CurrentClips.Length)];
            source.clip = currentClip;

            resolvedVolume = ResolveVolume(audioDataObject);
            source.volume = startSilent ? 0f : resolvedVolume;

            source.spatialBlend = isSpatial ? audioDataObject.SpatialBlend : 0f;

            poolAcquisitionService.SetPausePolicy(poolIndex, audioDataObject.RespectsGlobalPause);

            if (isSpatial)
            {
                poolObject.GameObject.transform.position = sourceTransform.position;
                poolAcquisitionService.SetFollowTarget(poolIndex, audioDataObject.FollowEmitter ? sourceTransform : null);
            }
            else
                poolAcquisitionService.SetFollowTarget(poolIndex, null);

            LowPassDispatchState lowPass = LowPassDispatchPolicy.Resolve(audioDataObject.UseWallCheck, defaultCutoffValue);
            filter.enabled = lowPass.Enabled;
            filter.cutoffFrequency = lowPass.CutoffFrequency;
            poolAcquisitionService.SetTargetCutoff(poolIndex, lowPass.CutoffFrequency);

            if (audioDataObject.IsOneShot)
            {
                poolAcquisitionService.SetSlotBusy(poolIndex, currentClip.length);
                source.PlayOneShot(currentClip);
                HonorActiveGlobalPause(source, audioDataObject, poolIndex);

                if (isSpatial && audioDataObject.UseWallCheck)
                    wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex);

                return poolIndex;
            }
            poolAcquisitionService.ResetSlotBusy(poolIndex);

            source.Play();
            HonorActiveGlobalPause(source, audioDataObject, poolIndex);

            if (isSpatial && audioDataObject.UseWallCheck)
                wallCheckService.StartWallCheckLoop(audioDataObject, poolIndex);
            return poolIndex;
        }

        private float ResolveVolume(AudioDataObject audioDataObject)
        {
            if (dictionaryProvider.VolumeDictionary.TryGetValue(audioDataObject.CurrentType, out float curVolume))
                return curVolume;

            Debug.LogWarning($"[AudioTool] No volume configured for type '{audioDataObject.CurrentType}' on '{audioDataObject.name}'. Falling back to 1.0.", audioDataObject);
            return 1f;
        }

        private AudioHandle Gate(int poolIndex, AudioDataObject audioDataObject)
            => poolIndex >= 0 && audioDataObject.CanHandleAudioSource ? MakeHandle(poolIndex) : AudioHandle.Invalid;

      
        public AudioHandle MakeHandle(int poolIndex) => new AudioHandle(poolIndex, poolAcquisitionService.CurrentGeneration(poolIndex));

        private void HonorActiveGlobalPause(AudioSource source, AudioDataObject audioDataObject, int poolIndex)
        {
            if (pauseService.IsGloballyPaused && audioDataObject.RespectsGlobalPause)
            {
                source.Pause();
                poolAcquisitionService.MarkSlotPaused(poolIndex);
            }
        }

        public void StopAudio(AudioHandle handle)
        {
            if (!poolAcquisitionService.IsHandleCurrent(handle)) return;
           
            fadeService.ClearFade(handle.PoolIndex);
            stopService.StopSlot(handle.PoolIndex);
        }
    }
}
