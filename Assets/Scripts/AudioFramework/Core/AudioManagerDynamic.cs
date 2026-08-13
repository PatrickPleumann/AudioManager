using UnityEngine;
using UnityEngine.SceneManagement;

using AudioFramework.Configuration;
using AudioFramework.Services.WallCheck;
using AudioFramework.Services.Playback;
using AudioFramework.Services.Following;
using AudioFramework.Services.Fading;
using AudioFramework.Services.Mixing;
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
        private AudioDuckService duckService;
        private AudioVolumeWriteService volumeWriteService;
        private CategoryVolumeSource categoryVolumeSource;
        private CategoryVolumeWriter categoryVolumeWriter;

        /// <summary>
        /// The scene this manager was loaded with, captured BEFORE it is made persistent. Once
        /// <see cref="MakePersistentAcrossScenes"/> has run, Unity has moved the GameObject into its internal
        /// "DontDestroyOnLoad" scene, so <c>gameObject.scene</c> no longer answers "where did I come from?" — and a
        /// later duplicate could never be told apart from an expected one. Do not move this assignment below it.
        /// </summary>
        private Scene originScene;

        /// <summary>
        /// Shown when the static API is called with no manager alive. Names both fixes, because with a persistent
        /// manager there are two valid setups: one manager per scene, or a single one in the first scene. The
        /// second is exactly what produces this message when a later scene is play-tested on its own.
        /// </summary>
        private const string NoManagerWarning =
            "[AudioTool] No AudioManagerDynamic found. Add one to this scene, or enter play mode from the scene " +
            "that has it — the manager survives scene loads, so a single one in your first scene covers the whole game.";

        private void Awake()
        {
            if (instance != null)
            {
                DestroySelfAsDuplicate();
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
                Debug.LogWarning("[AudioTool] No AudioListener found yet (it usually sits on the Main Camera). " +
                                 "The manager keeps running and picks one up by itself as soon as it appears — " +
                                 "this is normal for a bootstrap scene that loads its levels afterwards. Until " +
                                 "then 3D sounds have nothing to be positioned against; 2D sounds are unaffected.", this);
            }

            originScene = gameObject.scene;
            instance = this;
            MakePersistentAcrossScenes();

            dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(systemConfig.WallDampingPerLayer);
            dictionaryProvider.FillDictionaryWithKeysAndValues(systemConfig.CategoryVolumes);

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
                fadeTargets[i] = new PooledFadeTarget(stopService, pool, i);

            fadeService = new AudioFadeService(fadeTargets);

            duckService = systemConfig.EnableDucking ? new AudioDuckService(pool, systemConfig) : null;

            var volumeTargets = new IVolumeTarget[pool.Length];

            for (int i = 0; i < volumeTargets.Length; i++)
                volumeTargets[i] = new PooledVolumeTarget(pool, i);

            categoryVolumeSource = new CategoryVolumeSource(dictionaryProvider.VolumeDictionary);
            categoryVolumeWriter = new CategoryVolumeWriter(dictionaryProvider.VolumeDictionary);

            volumeWriteService = new AudioVolumeWriteService(
                volumeTargets,
                categoryVolumeSource,
                duckService
            );

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

        /// <summary>
        /// Handles a second manager meeting the one that is already running. One manager per scene is the CORRECT
        /// setup — without it a scene cannot be play-tested on its own — so with a persistent manager this path runs
        /// on every single scene load. It therefore stays SILENT for that case; warning there would train the user
        /// to ignore this tool's console output. Only two managers in the same scene are a real mistake.
        /// <para>
        /// Removes only this COMPONENT whenever the GameObject carries anything else: the manager may share its
        /// object with the user's own scripts, and tearing that down over an expected duplicate would delete their
        /// work. A GameObject holding nothing but this component — the recommended setup — is removed as a whole,
        /// so the recommended case leaves no empty leftover in the hierarchy either.
        /// </para>
        /// </summary>
        private void DestroySelfAsDuplicate()
        {
            if (gameObject.scene == instance.originScene)
            {
                Debug.LogWarning($"[AudioTool] Two AudioManagerDynamic components exist in the scene " +
                                 $"'{gameObject.scene.name}'. Only the first one runs, this one is removed. " +
                                 "Keep a single manager per scene.", this);
            }
            else if (systemConfig != null && systemConfig != instance.systemConfig)
            {
                Debug.LogWarning($"[AudioTool] The AudioManagerDynamic in '{gameObject.scene.name}' points at a " +
                                 $"different AudioSystemConfig ('{systemConfig.name}') than the manager already " +
                                 $"running ('{instance.systemConfig.name}'). The running one keeps its config: the " +
                                 "manager survives scene loads, so the first config that loads is the one in use.", this);
            }

            const int transformPlusThisComponent = 2;

            bool objectExistsOnlyForThisManager = transform.childCount == 0
                                                  && GetComponents<Component>().Length <= transformPlusThisComponent;

            if (objectExistsOnlyForThisManager)
            {
                Destroy(gameObject);
                return;
            }

            Destroy(this);
        }

        /// <summary>
        /// Lets the manager — and with it the pooled AudioSources, which are its children — survive a scene change, so
        /// music and ambience continue across a load instead of being cut off mid-clip. Called only for the instance
        /// that actually takes over, so a duplicate or a misconfigured manager is never made immortal.
        /// <para>
        /// Unity applies DontDestroyOnLoad to ROOT GameObjects only; on a nested manager the call does nothing but log
        /// an easily missed engine warning. That case is therefore reported with an actionable message and the call is
        /// skipped, rather than leaving the user with a documented persistence promise that never takes effect.
        /// </para>
        /// </summary>
        private void MakePersistentAcrossScenes()
        {
            if (transform.parent != null)
            {
                Debug.LogWarning(
                    $"[AudioTool] The AudioManagerDynamic GameObject '{name}' is a child of '{transform.parent.name}'. " +
                    "Unity's DontDestroyOnLoad only works on root GameObjects, so this manager will NOT survive a scene " +
                    "change and every playing sound is cut off on load. Move it to the top level of the Hierarchy.", this);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            followService?.UpdateFollowers();
            occlusionSmoothingService?.Tick(Time.unscaledDeltaTime);
            fadeService?.Tick(Time.unscaledDeltaTime);
            duckService?.Tick(Time.unscaledDeltaTime);
            //the volume write must stay last - it consumes the factors every service above resolved this frame
            volumeWriteService?.Apply();
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
                Debug.LogWarning(NoManagerWarning);
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
                Debug.LogWarning(NoManagerWarning);
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
                Debug.LogWarning(NoManagerWarning);
                return AudioHandle.Invalid;
            }

            int poolIndex = instance.playbackService.DispatchSilentNonSpatial(_data);
            if (poolIndex < 0) return AudioHandle.Invalid;

            instance.fadeService.StartFade(poolIndex, from: 0f, to: 1f, duration: _duration, stopOnEnd: false);
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
                Debug.LogWarning(NoManagerWarning);
                return AudioHandle.Invalid;
            }

            int poolIndex = instance.playbackService.DispatchSilentSpatial(_data, _source);
            if (poolIndex < 0) return AudioHandle.Invalid;

            instance.fadeService.StartFade(poolIndex, from: 0f, to: 1f, duration: _duration, stopOnEnd: false);
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

        /// <summary>
        /// Sets the base volume of an entire category at runtime — this is what a settings-menu slider calls.
        /// Applies to sounds that are ALREADY playing, not just to new ones: the value is read live while the gain
        /// is resolved, so the change is audible on the next frame.
        /// <para>
        /// Values outside [0, 1] are clamped rather than rejected, so what <see cref="GetCategoryVolume"/> reads
        /// back is always what the player actually hears. A category with no configured entry is created on the
        /// fly and reported — that almost always means a missing entry in the config's category volume list, not
        /// an intended runtime addition.
        /// </para>
        /// <para>
        /// The change lives for this session only; the AudioSystemConfig asset is never written. Persisting the
        /// player's choice (PlayerPrefs or your own save system) is deliberately left to your game.
        /// </para>
        /// </summary>
        /// <param name="_category">The category whose base volume changes.</param>
        /// <param name="_volume">The new base volume, clamped to [0, 1].</param>
        public static void SetCategoryVolume(AudioCategory _category, float _volume)
        {
            if (instance == null) return;

            if (instance.categoryVolumeWriter.Set(_category, _volume) == CategoryVolumeWriteOutcome.EntryCreated)
                Debug.LogWarning($"[AudioTool] Category '{_category}' had no volume entry and was created at runtime. " +
                                 "Add it to the category volume list in your AudioSystemConfig so it starts at the " +
                                 "value you configured instead of at full volume.");
        }

        /// <summary>
        /// The current base volume of a category — what a settings slider shows when the menu opens. Returns 1.0
        /// for a category that has no configured entry, and also while no manager is alive, so a menu that builds
        /// itself before the manager wakes up starts at full volume instead of at silence.
        /// </summary>
        /// <param name="_category">The category to read.</param>
        public static float GetCategoryVolume(AudioCategory _category)
        {
            if (instance == null) return 1f;

            return instance.categoryVolumeSource.For(_category);
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            wallCheckService?.StopAllChecks();
            instance = null;
        }
    }
}
