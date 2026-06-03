using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Configuration;
using AudioFramework.Services.WallCheck;
using AudioFramework.Services.Playback;
using AudioFramework.Data;
using AudioFramework.Pause;
using AudioFramework.Pooling;
using AudioFramework.Utilities;
using AudioFramework.Interfaces;

public class AudioManagerDynamic : MonoBehaviour
{
    [Header("--- System Config ---")]
    [SerializeField] private AudioSystemConfig systemConfig;

    private static AudioManagerDynamic instance;

    private readonly AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();

    private IAudioWallCheckService wallCheckService;
    private AudioPoolAcquisitionService poolAcquisitionService;
    private AudioPauseService pauseService;
    private AudioPlaybackService playbackService;

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

        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(systemConfig.CutOffFrequenciesPerLayer);
        dictionaryProvider.FillDictionaryWithKeysAndValues(systemConfig.TransferObject);

        Transform playerAudioListenerTransform = audioListener.transform;

        poolAcquisitionService = new AudioPoolAcquisitionService(systemConfig, transform);
        pauseService = new AudioPauseService(poolAcquisitionService.PoolArray);

#if !USE_UNITASK
        wallCheckService = new AudioCoroutineWallCheckService(poolAcquisitionService.PoolArray, systemConfig, playerAudioListenerTransform, dictionaryProvider, this);
        Debug.Log("[AudioTool] Internal Coroutine mode was initialized (not recommended)");
#else
        wallCheckService = new AudioUniTaskWallCheckService(poolAcquisitionService.PoolArray, systemConfig, playerAudioListenerTransform, dictionaryProvider);
        Debug.Log("[AudioTool] UniTask mode was initialized (recommended)");
#endif

        playbackService = new AudioPlaybackService(
            poolAcquisitionService,
            dictionaryProvider,
            wallCheckService,
            systemConfig.defaultCuttoffFreqValue
        );
    }

    /// <summary>
    /// Plays a sound as positional 3D audio at <paramref name="source"/>. The sound is attenuated by distance and,
    /// if enabled on the ADO, wall-checked. Use this for anything that happens at a place in the world (footsteps,
    /// gunshots, enemy voice lines). The actual 3D-ness is governed by the ADO's SpatialBlend field (1 = full 3D).
    /// </summary>
    /// <param name="data">The AudioDataObject — WHAT to play (clips, volume type, spatial blend, flags).</param>
    /// <param name="source">The Transform the sound originates from — WHERE it plays. Must not be null for 3D.</param>
    /// <returns>A valid AudioHandle when the ADO has CanHandleAudioSource enabled and a slot was free; otherwise an invalid handle.</returns>
    public static AudioHandle PlaySpatial(AudioDataObject data, Transform source)
    {
        if (instance == null)
        {
            Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
            return new AudioHandle(-1);
        }
        return instance.playbackService.DispatchAudio(data, source);
    }

    /// <summary>
    /// Convenience overload that plays a <see cref="SoundRequest"/> (ADO + source Transform bundled together) as
    /// positional 3D audio. Intended for event-driven dispatch, where the request travels through an event as a
    /// single payload and is handed straight to this method.
    /// </summary>
    /// <param name="request">The bundled sound request (WHAT + WHERE).</param>
    public static AudioHandle PlaySpatial(SoundRequest request) => PlaySpatial(request.Ado, request.Source);

    /// <summary>
    /// Plays a sound as NON-spatial 2D audio. The sound has no position: it ignores distance, ignores wall-check,
    /// and plays everywhere at equal level (spatialBlend is forced to 0, regardless of the ADO's SpatialBlend field).
    /// Use this for UI clicks, music and global stingers.
    /// <para>
    /// IMPORTANT: Passing only an ADO never produces 3D audio. For positional 3D sound use
    /// <see cref="PlaySpatial(AudioDataObject, Transform)"/> and supply a source Transform.
    /// </para>
    /// </summary>
    /// <param name="data">The AudioDataObject — WHAT to play.</param>
    public static AudioHandle PlayNonSpatial(AudioDataObject data)
    {
        if (instance == null)
        {
            Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
            return new AudioHandle(-1);
        }
        return instance.playbackService.DispatchAudioNonSpatial(data);
    }

    public static void Stop(AudioHandle handle) => instance?.playbackService.StopAudio(handle);

    public static void PauseAll() => instance?.pauseService?.PauseAll();

    public static void UnpauseAll() => instance?.pauseService?.UnpauseAll();

    private void OnDestroy()
    {
        wallCheckService?.StopAllChecks();
        instance = null;
    }
}
