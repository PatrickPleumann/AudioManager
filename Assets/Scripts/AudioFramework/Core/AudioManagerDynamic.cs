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

    public static AudioHandle Play(AudioDataObject data)
    {
        if (instance == null)
        {
            Debug.LogWarning("[AudioTool] No AudioManagerDynamic found in scene.");
            return new AudioHandle(-1);
        }
        return instance.playbackService.DispatchAudio(data);
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
