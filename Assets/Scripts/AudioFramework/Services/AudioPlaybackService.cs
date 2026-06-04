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
                return new AudioHandle(-1);
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

        // Returns the raw pool index of the dispatched slot, or -1 on failure. Turning that into a user-facing
        // AudioHandle (gated by CanHandleAudioSource) is done by the public Play callers via Gate(), so the fade
        // path can still get the index even when an ADO opts out of handles.
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

            // A recycled slot must never carry a previous fade, or the next Tick would write a stale volume over this
            // new sound. Cleared every dispatch (control-surface principle), same as pause/follow state.
            fadeService.ClearFade(poolIndex);

            AudioObject poolObject = poolAcquisitionService.PoolArray[poolIndex];
            AudioSource source = poolObject.Source;
            AudioLowPassFilter filter = poolObject.Filter;

            AudioClip currentClip = audioDataObject.CurrentClips[Random.Range(0, audioDataObject.CurrentClips.Length)];
            source.clip = currentClip;

            resolvedVolume = ResolveVolume(audioDataObject);
            // Always written, so a pooled slot never carries the previous sound's volume. A fade-in starts silent and
            // the fade service ramps source.volume up to resolvedVolume.
            source.volume = startSilent ? 0f : resolvedVolume;

            // Always written, so a pooled slot never carries the previous sound's spatialization.
            source.spatialBlend = isSpatial ? audioDataObject.SpatialBlend : 0f;

            // Always written (control-surface), so a reused slot can't keep the previous sound's pause policy.
            poolAcquisitionService.SetPausePolicy(poolIndex, audioDataObject.RespectsGlobalPause);

            if (isSpatial)
            {
                poolObject.GameObject.transform.position = sourceTransform.position;
                // Follow via tracked position, never via SetParent: the pooled slot must never become a child of a
                // caller-owned object, or destroying that caller would destroy the pooled AudioObject. AudioFollowService
                // copies the position each frame while playing. Written every dispatch so a reused slot can't keep
                // following a previous emitter (control-surface principle).
                poolAcquisitionService.SetFollowTarget(poolIndex, audioDataObject.FollowEmitter ? sourceTransform : null);
            }
            else
                poolAcquisitionService.SetFollowTarget(poolIndex, null);

            filter.cutoffFrequency = defaultCutoffValue;

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

            // Fallback: full volume, so a pooled slot never carries the previous sound's volume on a dictionary miss.
            Debug.LogWarning($"[AudioTool] No volume configured for type '{audioDataObject.CurrentType}' on '{audioDataObject.name}'. Falling back to 1.0.", audioDataObject);
            return 1f;
        }

        // Turn a raw pool index into the user-facing handle: valid only when a slot was acquired AND the ADO opts into
        // handle-based control. Mirrors the original gating that lived at the end of Dispatch.
        private static AudioHandle Gate(int poolIndex, AudioDataObject audioDataObject)
            => poolIndex >= 0 && audioDataObject.CanHandleAudioSource ? new AudioHandle(poolIndex) : new AudioHandle(-1);

        // If a global PauseAll() is currently in effect, a sound that respects the global pause must start paused too
        // (matches AudioListener.pause semantics), so it stays silent until UnpauseAll() instead of blaring through the
        // pause. Play()-then-Pause() in the same frame produces no audible output. Sounds that ignore the global pause
        // (RespectsGlobalPause == false, e.g. UI/music) are left playing.
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
            if (!handle.IsValid) return;
            // Cancel any in-progress fade first, then stop the audio — a hard Stop means stop now, so the fade must
            // not keep writing volume to a slot that may get reused.
            fadeService.ClearFade(handle.PoolIndex);
            stopService.StopSlot(handle.PoolIndex);
        }
    }
}
