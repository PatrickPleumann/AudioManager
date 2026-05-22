#define USE_UNITASK
using System;
using System.Threading;
using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Configuration;
using AudioFramework.Services.WallCheck;
using AudioFramework.Services.Playback;
using AudioFramework.Data;
using AudioFramework.Pause;
using AudioFramework.Pooling;
using AudioFramework.Utilities;

public class AudioManagerDynamic : MonoBehaviour
{
    [Header("--- System Config ---")]
    [SerializeField] private AudioSystemConfigSO systemConfig;
    [SerializeField] private Transform playerAudioListenerTransform;

    private readonly AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();

    private IAudioWallCheckService wallCheckService;
    private AudioPoolAcquisitionService poolAcquisitionService;
    private AudioPauseService pauseService;
    private AudioPlaybackService playbackService;

    public static Func<AudioDataObject, AudioHandle> AquireFreeAudioSource;
    public static Action<AudioHandle> HandledAudioSourceStop;

    public static Action GlobalPauseAllEvent;
    public static Action GlobalUnpauseAllEvent;

#if USE_UNITASK
    private CancellationTokenSource[] poolTokenSources;
    private CancellationTokenSource linkedMasterTokenSource;
#endif

    private void Awake()
    {
        if (systemConfig == null) return;

        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(systemConfig.CutOffFrequenciesPerLayer);
        dictionaryProvider.FillDictionaryWithKeysAndValues(systemConfig.TransferObject);

        playerAudioListenerTransform = FindFirstObjectByType<AudioListener>().transform;

        poolAcquisitionService = new AudioPoolAcquisitionService(systemConfig, transform);
        pauseService = new AudioPauseService(poolAcquisitionService.PoolArray);

#if !USE_UNITASK
        wallCheckService = new AudioCoroutineWallCheckService(poolAcquisitionService.PoolArray, systemConfig, playerAudioListenerTransform, dictionaryProvider, this);
        Debug.Log("[AudioTool] Internal Coroutine mode was initialized (not recommended)");
#else
        wallCheckService = new AudioUniTaskWallCheckService(poolAcquisitionService.PoolArray, systemConfig, playerAudioListenerTransform, dictionaryProvider);
        Debug.Log("[AudioTool] UniTask mode was initialized (recommended)");

        poolTokenSources = new CancellationTokenSource[systemConfig.NumbersOfAudioSources];
        linkedMasterTokenSource = new CancellationTokenSource();
        for (int i = 0; i < poolTokenSources.Length; i++) poolTokenSources[i] = new CancellationTokenSource();
#endif

        playbackService = new AudioPlaybackService(
            poolAcquisitionService,
            dictionaryProvider,
            wallCheckService,
            systemConfig.defaultCuttoffFreqValue
        );
    }

    private void OnEnable()
    {
        AquireFreeAudioSource += DispatchAudioFromBus;
        HandledAudioSourceStop += StopAudioFromBus;

        GlobalPauseAllEvent += PauseAllSources;
        GlobalUnpauseAllEvent += UnpauseAllSources;
    }

    private void OnDisable()
    {
        AquireFreeAudioSource -= DispatchAudioFromBus;
        HandledAudioSourceStop -= StopAudioFromBus;

        GlobalPauseAllEvent -= PauseAllSources;
        GlobalUnpauseAllEvent -= UnpauseAllSources;
    }

    private AudioHandle DispatchAudioFromBus(AudioDataObject data)
    {
#if USE_UNITASK
        if (data != false && data.UseWallCheck)
        {
            int poolIndex = poolAcquisitionService.GetFreePoolIndex();
            if (poolIndex != -1)
            {
                poolTokenSources[poolIndex].Cancel();
                poolTokenSources[poolIndex].Dispose();
                poolTokenSources[poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(linkedMasterTokenSource.Token);
            }
        }
#endif
        return playbackService.DispatchAudio(data);
    }

    private void StopAudioFromBus(AudioHandle handle)
    {
#if USE_UNITASK
        if (handle.IsValid) poolTokenSources[handle.PoolIndex].Cancel();
#endif
        playbackService.StopAudio(handle);
    }

    public void PauseAllSources() => pauseService?.PauseAll();
    public void UnpauseAllSources() => pauseService?.UnpauseAll();

    private void OnDestroy()
    {
        wallCheckService?.StopAllChecks();
#if USE_UNITASK
        if (linkedMasterTokenSource != null)
        {
            linkedMasterTokenSource.Cancel();
            linkedMasterTokenSource.Dispose();
        }
        if (poolTokenSources != null)
        {
            for (int i = 0; i < poolTokenSources.Length; i++)
                if (poolTokenSources[i] != null) poolTokenSources[i].Dispose();
        }
#endif
    }
}