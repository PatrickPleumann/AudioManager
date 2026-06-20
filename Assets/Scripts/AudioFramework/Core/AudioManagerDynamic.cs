using UnityEngine;

using AudioFramework.Configuration;
using AudioFramework.Services.WallCheck;
using AudioFramework.Services.Playback;
using AudioFramework.Services.Following;
using AudioFramework.Services.Fading;
using AudioFramework.Data;
using AudioFramework.Pause;
using AudioFramework.Pooling;
using AudioFramework.Utilities;
using AudioFramework.Interfaces;

namespace AudioFramework.Core
{
    public class AudioManagerDynamic : MonoBehaviour
    {
        [Header("--- System Config ---")]
        [SerializeField] private AudioSystemConfig systemConfig;

        private static AudioManagerDynamic instance;

        private readonly AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();

        private IAudioWallCheckService wallCheckService;
        private AudioPoolAcquisitionService poolAcquisitionService;
        private AudioPauseService pauseService;
        private AudioStopService stopService;
        private AudioPlaybackService playbackService;
        private AudioFollowService followService;
        private AudioOcclusionSmoothingService occlusionSmoothingService;
        private AudioFadeService fadeService;

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning("[AudioTool] Multiple AudioManagerDynamic instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            if (systemConfig == null)
            {
                Debug.LogError("[AudioTool] No AudioSystemConfig assigned. AudioManager is disabled.", this);
                enabled = false;
                return;
            }

            AudioListener audioListener = FindFirstObjectByType<AudioListener>();
            if (audioListener == null)
            {
                Debug.LogError("[AudioTool] No AudioListener found in the scene (usually on the Main Camera). AudioManager is disabled.", this);
                enabled = false;
                return;
            }

            instance = this;

            dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(systemConfig.WallDampingPerLayer);
            dictionaryProvider.FillDictionaryWithKeysAndValues(systemConfig.TransferObject);

            IAudioListenerProvider listenerProvider = new SceneAudioListenerProvider(audioListener);

            poolAcquisitionService = new AudioPoolAcquisitionService(systemConfig, transform);
            pauseService = new AudioPauseService(poolAcquisitionService.PoolArray);

#if !USE_UNITASK
            wallCheckService = new AudioCoroutineWallCheckService(poolAcquisitionService.PoolArray, systemConfig, listenerProvider, dictionaryProvider, this);
            Debug.Log("[AudioTool] Internal Coroutine mode was initialized (not recommended)");
#else
            wallCheckService = new AudioUniTaskWallCheckService(poolAcquisitionService.PoolArray, systemConfig, listenerProvider, dictionaryProvider);
            Debug.Log("[AudioTool] UniTask mode was initialized (recommended)");
#endif

            stopService = new AudioStopService(poolAcquisitionService, wallCheckService);

            AudioObject[] pool = poolAcquisitionService.PoolArray;
            var fadeTargets = new IFadeTarget[pool.Length];
            for (int i = 0; i < fadeTargets.Length; i++)
                fadeTargets[i] = new PooledFadeTarget(pool[i].Source, stopService, pool, i);
            fadeService = new AudioFadeService(fadeTargets);

            playbackService = new AudioPlaybackService(
                poolAcquisitionService,
                dictionaryProvider,
                wallCheckService,
                pauseService,
                stopService,
                fadeService,
                systemConfig.DefaultCutoffFreqValue
            );

            followService = new AudioFollowService(poolAcquisitionService, wallCheckService, fadeService);
            occlusionSmoothingService = new AudioOcclusionSmoothingService(poolAcquisitionService, systemConfig);
        }

        private void LateUpdate()
        {
            followService?.UpdateFollowers();
            // Fades and occlusion glides are real-time volume/cutoff animations on AudioSources, whose playback is
            // NOT affected by Time.timeScale. Driving them with unscaledDeltaTime keeps them consistent with that
            // playback and decouples them from timeScale: a Time.timeScale = 0 pause no longer freezes them (that is
            // exclusively PauseAll's job, via the IsPaused gate), and they run in real seconds in slow-mo/fast-forward.
            occlusionSmoothingService?.Tick(Time.unscaledDeltaTime);
            fadeService?.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Plays a sound as positional 3D audio at <paramref name="_source"/>. The sound is attenuated by distance and,
        /// if enabled on the ADO, wall-checked. Use this for anything that happens at a place in the world (footsteps,
        /// gunshots, enemy voice lines). The actual 3D-ness is governed by the ADO's SpatialBlend field (1 = full 3D).
        /// </summary>
        /// <param name="_data">The AudioDataObject — WHAT to play (clips, volume type, spatial blend, flags).</param>
        /// <param name="_source">The Transform the sound originates from — WHERE it plays. Must not be null for 3D.</param>
        /// <returns>A valid AudioHandle when the ADO has CanHandleAudioSource enabled and a slot was free; otherwise an invalid handle.</returns>
        public static AudioHandle PlaySpatial(AudioDataObject _data, Transform _source)
        {
            if (instance == null)
            {
                Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
                return AudioHandle.Invalid;
            }
            return instance.playbackService.DispatchAudio(_data, _source);
        }

        /// <summary>
        /// Convenience overload that plays a <see cref="SoundRequest"/> (ADO + source Transform bundled together) as
        /// positional 3D audio. Intended for event-driven dispatch, where the request travels through an event as a
        /// single payload and is handed straight to this method.
        /// </summary>
        /// <param name="_request">The bundled sound request (WHAT + WHERE).</param>
        public static AudioHandle PlaySpatial(SoundRequest _request) => PlaySpatial(_request.Ado, _request.Source);

        /// <summary>
        /// Plays a sound as NON-spatial 2D audio. The sound has no position: it ignores distance, ignores wall-check,
        /// and plays everywhere at equal level (spatialBlend is forced to 0, regardless of the ADO's SpatialBlend field).
        /// Use this for UI clicks, music and global stingers.
        /// <para>
        /// IMPORTANT: Passing only an ADO never produces 3D audio. For positional 3D sound use
        /// <see cref="PlaySpatial(AudioDataObject, Transform)"/> and supply a source Transform.
        /// </para>
        /// </summary>
        /// <param name="_data">The AudioDataObject — WHAT to play.</param>
        public static AudioHandle PlayNonSpatial(AudioDataObject _data)
        {
            if (instance == null)
            {
                Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
                return AudioHandle.Invalid;
            }
            return instance.playbackService.DispatchAudioNonSpatial(_data);
        }

        public static void Stop(AudioHandle _handle) => instance?.playbackService.StopAudio(_handle);

        /// <summary>
        /// Plays a NON-spatial 2D sound that fades IN from silence up to its category volume over
        /// <paramref name="_duration"/> seconds. Returns a handle to the faded sound so it can later be stopped, faded
        /// out or crossfaded — a fade is always a managed sound, so (unlike PlayNonSpatial) the handle does NOT depend
        /// on the ADO's CanHandleAudioSource.
        /// </summary>
        public static AudioHandle FadeInNonSpatial(AudioDataObject _data, float _duration)
        {
            if (instance == null)
            {
                Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
                return AudioHandle.Invalid;
            }

            int poolIndex = instance.playbackService.DispatchSilentNonSpatial(_data, out float targetVolume);
            if (poolIndex < 0) return AudioHandle.Invalid;

            instance.fadeService.StartFade(poolIndex, from: 0f, to: targetVolume, duration: _duration, stopOnEnd: false);
            return instance.playbackService.MakeHandle(poolIndex);
        }

        /// <summary>
        /// Fades an already-playing sound OUT to silence over <paramref name="_duration"/> seconds, then stops it.
        /// No-op if the handle is invalid. Works for spatial and non-spatial sounds alike — it just ramps the existing
        /// slot down, so it needs no spatial variant.
        /// </summary>
        public static void FadeOut(AudioHandle _handle, float _duration)
        {
            if (instance == null) return;

            if (!instance.poolAcquisitionService.IsHandleCurrent(_handle)) return;
            instance.fadeService.StartFadeOut(_handle.PoolIndex, _duration);
        }

        /// <summary>
        /// Crossfades into a new NON-spatial sound over <paramref name="_duration"/> seconds: the old sound
        /// (<paramref name="_from"/>) fades out and stops while the new one (<paramref name="_to"/>) fades in.
        /// Composition of FadeOut + FadeInNonSpatial. If <paramref name="_from"/> is invalid, only the fade-in runs.
        /// </summary>
        public static AudioHandle CrossfadeNonSpatial(AudioHandle _from, AudioDataObject _to, float _duration)
        {
            FadeOut(_from, _duration);
            return FadeInNonSpatial(_to, _duration);
        }

        /// <summary>
        /// Plays a positional 3D sound at <paramref name="_source"/> that fades IN from silence up to its category
        /// volume over <paramref name="_duration"/> seconds. Spatial counterpart of <see cref="FadeInNonSpatial"/>;
        /// always returns a valid handle (a fade is a managed sound). Must not be called with a null source.
        /// </summary>
        public static AudioHandle FadeInSpatial(AudioDataObject _data, Transform _source, float _duration)
        {
            if (instance == null)
            {
                Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
                return AudioHandle.Invalid;
            }

            int poolIndex = instance.playbackService.DispatchSilentSpatial(_data, _source, out float targetVolume);
            if (poolIndex < 0) return AudioHandle.Invalid;

            instance.fadeService.StartFade(poolIndex, from: 0f, to: targetVolume, duration: _duration, stopOnEnd: false);
            return instance.playbackService.MakeHandle(poolIndex);
        }

        /// <summary>
        /// Crossfades into a new positional 3D sound at <paramref name="_source"/> over <paramref name="_duration"/>
        /// seconds: the old sound (<paramref name="_from"/>) fades out and stops while the new one (<paramref name="_to"/>)
        /// fades in at its position. Composition of FadeOut + FadeInSpatial — e.g. a looping engine crossfading into a
        /// positional engine-cutout sound at the same place. If <paramref name="_from"/> is invalid, only the fade-in runs.
        /// </summary>
        public static AudioHandle CrossfadeSpatial(AudioHandle _from, AudioDataObject _to, Transform _source, float _duration)
        {
            FadeOut(_from, _duration);
            return FadeInSpatial(_to, _source, _duration);
        }

        public static void PauseAll() => instance?.pauseService?.PauseAll();

        public static void UnpauseAll() => instance?.pauseService?.UnpauseAll();

        private void OnDestroy()
        {
            if (instance != this) return;

            wallCheckService?.StopAllChecks();
            instance = null;
        }
    }
}
