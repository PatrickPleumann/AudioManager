using System;
using System.Threading;
using UnityEngine;

public class AudioManagerDynamicMerged_Test : MonoBehaviour
{
    [Header("--- System Konfiguration ---")]
    [SerializeField] private AudioSystemConfigSO_Test systemConfig;
    [SerializeField] private Transform playerAudioListenerTransform;

    private AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();

    private IAudioWallCheckService_Test wallCheckService;
    private AudioPoolAcquisitionService_Test poolAcquisitionService;
    private AudioPauseService_Test pauseService;
    private AudioPlaybackService_Test playbackService;

    public static Func<AudioDataObject, AudioHandle_Test> CallAudioSourceDispatcher;
    public static Action<AudioHandle_Test> DynamicAudioSourceStop;

    public static Action GlobalPauseAllEvent;
    public static Action GlobalUnpauseAllEvent;

#if USE_UNITASK
    private CancellationTokenSource[] _poolTokenSources;
    private CancellationTokenSource _linkedMasterTokenSource;
#endif

    private void Awake()
    {
        if (systemConfig == null) return;

        poolAcquisitionService = new AudioPoolAcquisitionService_Test(systemConfig, this.transform);
        pauseService = new AudioPauseService_Test(poolAcquisitionService.PoolArray);

#if !USE_UNITASK
        wallCheckService = new AudioCoroutineWallCheckService_Test(poolAcquisitionService.PoolArray, systemConfig, playerAudioListenerTransform, dictionaryProvider, this);
#else
        _wallCheckService = new AudioUniTaskWallCheckService_Test(_poolAcquisitionService.PoolArray, systemConfig, playerAudioListenerTransform, dictionaryProvider);
        
        _poolTokenSources = new CancellationTokenSource[systemConfig.numbersOfAudioSources];
        _linkedMasterTokenSource = new CancellationTokenSource();
        for (int i = 0; i < _poolTokenSources.Length; i++) _poolTokenSources[i] = new CancellationTokenSource();
#endif

        playbackService = new AudioPlaybackService_Test(
            poolAcquisitionService,
            dictionaryProvider,
            wallCheckService,
            systemConfig.defaultCuttoffFreqValue
        );
    }

    private void Start()
    {
        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(systemConfig.CutOffFrequenciesPerLayer);
        dictionaryProvider.FillDictionaryWithKeysAndValues(systemConfig.transferObject);

        CallAudioSourceDispatcher += DispatchAudioFromBus;
        DynamicAudioSourceStop += StopAudioFromBus;

        GlobalPauseAllEvent += PauseAllSources;
        GlobalUnpauseAllEvent += UnpauseAllSources;
    }

    private void OnDisable()
    {
        CallAudioSourceDispatcher -= DispatchAudioFromBus;
        DynamicAudioSourceStop -= StopAudioFromBus;
    }

    private AudioHandle_Test DispatchAudioFromBus(AudioDataObject data)
    {
#if USE_UNITASK
        if (data != null && data.useWallCheck)
        {
            int poolIndex = _poolAcquisitionService.GetFreePoolIndex();
            if (poolIndex != -1)
            {
                _poolTokenSources[poolIndex].Cancel();
                _poolTokenSources[poolIndex].Dispose();
                _poolTokenSources[poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(_linkedMasterTokenSource.Token);
            }
        }
#endif
        return playbackService.DispatchAudio(data);
    }

    private void StopAudioFromBus(AudioHandle_Test handle)
    {
#if USE_UNITASK
        if (handle.IsValid) _poolTokenSources[handle.PoolIndex].Cancel();
#endif
        playbackService.StopAudio(handle);
    }

    public void PauseAllSources() => pauseService?.PauseAll();
    public void UnpauseAllSources() => pauseService?.UnpauseAll();

    private void OnDestroy()
    {
        wallCheckService?.StopAllChecks();
#if USE_UNITASK
        if (_linkedMasterTokenSource != null)
        {
            _linkedMasterTokenSource.Cancel();
            _linkedMasterTokenSource.Dispose();
        }
        if (_poolTokenSources != null)
        {
            for (int i = 0; i < _poolTokenSources.Length; i++)
                if (_poolTokenSources[i] != null) _poolTokenSources[i].Dispose();
        }
#endif
    }
}