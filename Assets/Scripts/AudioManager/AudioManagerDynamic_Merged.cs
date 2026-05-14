#define USE_UNITASK //TODO: REMOVE THIS!!!
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif

public class AudioManagerDynamicMerged : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private AudioSourceProviderService sourceProvider;
    AudioPauseService audioPauseService;


    [Header("General Values for both Methods")]
    [SerializeField][Range(1, 100)] private int numbersOfAudioSources = 50;
    [SerializeField] private float defaultCuttoffFreqValue = 5000f;
    [SerializeField] private AudioVolumesTransferObject transferObject;
    [SerializeField] private GameObject audioGameObjectPrefab;
    [SerializeField] private CutoffFreqLayerBehaviour[] CutoffFrequenciesPerLayer;
    [SerializeField] private Transform playerAudioListenerTransform;
    [SerializeField][Range(0.01f, 1f)] private float timeIntervalBetweenPositionChecks = 0.25f;

    private LayerMask automaticallyGeneratedWallLayerMask;

    private AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();
    private PoolAudioObject[] allAudioSources;

    public static UnityAction<AudioDataObject> CallAudioSourceDispatcher;
    public static UnityAction<AudioDataObject> DynamicAudioSourceStop;
    public static UnityAction CallPauseAllSources;
    public static UnityAction CallUnPauseAllSources;


#if !USE_UNITASK
    [Header("Coroutine specific values")]
    private readonly Dictionary<int, Coroutine> activeCoroutineChecks = new Dictionary<int, Coroutine>();
    private WaitForSeconds intervalWait;
    private WaitForSeconds pauseWait;
#else
    [Header("UniTask specific values")]
    private CancellationTokenSource linkedMasterTokenSource;
#endif

    private void Awake()
    {
#if !USE_UNITASK
        intervalWait = new WaitForSeconds(timeIntervalBetweenPositionChecks);
        pauseWait = new WaitForSeconds(0.1f);
        Debug.Log("[AudioTool] Pure Coroutine mode initialized (Not Recommended).");
#else
        
        linkedMasterTokenSource = new CancellationTokenSource();
        Debug.Log("[AudioTool] UniTask mode initialized (Recommended).");
#endif
       audioPauseService = new(sourceProvider.allAudioSources);

    }

    private void OnEnable()
    {
        CallAudioSourceDispatcher += AquireAudioSource;
        DynamicAudioSourceStop += StopCurrentAudioSource;

        CallPauseAllSources += audioPauseService.PauseAllSources;
        CallUnPauseAllSources += audioPauseService.UnpauseAllSources;
    }

    private void Start()
    {
        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(CutoffFrequenciesPerLayer);
        dictionaryProvider.FillDictionaryWithKeysAndValues(transferObject);

        GenerateLayerMaskFromDictionary();
    }

    private void OnDisable()
    {
        CallAudioSourceDispatcher -= AquireAudioSource;
        DynamicAudioSourceStop -= StopCurrentAudioSource;

        CallPauseAllSources -= audioPauseService.PauseAllSources;
        CallUnPauseAllSources -= audioPauseService.UnpauseAllSources;
    }


    private void AquireAudioSource(AudioDataObject _audioDataObject)
    {
        int poolIndex = sourceProvider.GetFreeAudioPoolIndex();
        if (poolIndex == -1) return;

        _audioDataObject.PoolIndex = poolIndex;

        PoolAudioObject poolObject = sourceProvider.allAudioSources[poolIndex];
        AudioSource source = poolObject.Source;
        AudioLowPassFilter filter = poolObject.Filter;

        source.clip = _audioDataObject.CurrentClips[Random.Range(0, _audioDataObject.CurrentClips.Length)];

        if (dictionaryProvider.volumeDictionary.TryGetValue(_audioDataObject.CurrentType, out float curVolume))
            source.volume = curVolume;

        if (_audioDataObject.SetCallerAsParent)
        {
            poolObject.GameObject.transform.SetParent(_audioDataObject.CallerTransform);
            poolObject.GameObject.transform.position = _audioDataObject.CallerTransform.position;
        }
        else
            poolObject.GameObject.transform.position = _audioDataObject.CallerTransform.position;

        filter.cutoffFrequency = defaultCuttoffFreqValue;


#if !USE_UNITASK
        StartWallCheckCoroutine(_audioDataObject, filter, source.clip.length, poolIndex);
#else
        StartWallCheckUniTask(_audioDataObject, filter, source.clip.length, poolIndex);
#endif
        source.Play();
    }

#if !USE_UNITASK
    private void StartWallCheckCoroutine(AudioDataObject audioDataObject, AudioLowPassFilter filter, float currentClipLength, int poolIndex)
    {
        // Blitzschneller O(1) Dictionary-Zugriff direkt über den int-Key!
        if (activeCoroutineChecks.TryGetValue(poolIndex, out Coroutine runningCoroutine))
        {
            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
            activeCoroutineChecks.Remove(poolIndex);
        }

        Coroutine newCheck = StartCoroutine(CheckIfPlayerBehindWallRoutine(audioDataObject, filter, currentClipLength, poolIndex));
        activeCoroutineChecks.Add(poolIndex, newCheck);
    }

    private IEnumerator CheckIfPlayerBehindWallRoutine(AudioDataObject audioDataObject, AudioLowPassFilter filter, float currentClipLength, int poolIndex)
    {
        float elapsedPlayTime = 0f;
        AudioSource targetSource = sourceProvider.allAudioSources[poolIndex].Source;

        if (CheckIfPlayerIsBehindWall(audioDataObject, out RaycastHit firstHit))
            AssignNewCutoffFreqToCurrentSource(filter, firstHit);

        while (elapsedPlayTime < currentClipLength)
        {
            if (audioDataObject == null || targetSource == null) yield break;

            if (!targetSource.isPlaying)
            {
                yield return pauseWait;
                continue;
            }

            if (CheckIfPlayerIsBehindWall(audioDataObject, out RaycastHit tempHit))
                AssignNewCutoffFreqToCurrentSource(filter, tempHit);
            else if (filter != null)
                filter.cutoffFrequency = defaultCuttoffFreqValue;

            yield return intervalWait;
            elapsedPlayTime += timeIntervalBetweenPositionChecks;
        }

        if (audioDataObject != null) audioDataObject.PoolIndex = -1;
        activeCoroutineChecks.Remove(poolIndex);
    }
#endif


#if USE_UNITASK
    private void StartWallCheckUniTask(AudioDataObject _audioDataObject, AudioLowPassFilter _filter, float _currentClipLength, int _poolIndex)
    {
        sourceProvider.poolTokenSources[_poolIndex].Cancel();
        sourceProvider.poolTokenSources[_poolIndex].Dispose();
        sourceProvider.poolTokenSources[_poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(linkedMasterTokenSource.Token);

        CheckIfPlayerBehindWallUniTaskVoid(
            sourceProvider.poolTokenSources[_poolIndex].Token,
            _audioDataObject,
            _filter,
            _currentClipLength,
            _poolIndex
        ).Forget();
    }

    private async UniTaskVoid CheckIfPlayerBehindWallUniTaskVoid(CancellationToken _token, AudioDataObject _audioDataObject, AudioLowPassFilter _filter, float _currentClipLength, int _poolIndex)
    {
        float elapsedPlayTime = 0f;
        AudioSource targetSource = sourceProvider.allAudioSources[_poolIndex].Source;
        int checkIntervalMs = (int)(timeIntervalBetweenPositionChecks * 1000);

        if (CheckIfPlayerIsBehindWall(_audioDataObject, out RaycastHit firstHit))
            AssignNewCutoffFreqToCurrentSource(_filter, firstHit);

        while (elapsedPlayTime < _currentClipLength)
        {
            if (_token.IsCancellationRequested) return;
            if (_audioDataObject == null || targetSource == null) return;

            if (!targetSource.isPlaying)
            {
                bool isCanceled = await UniTask.Delay(100, delayType: DelayType.DeltaTime, cancellationToken: _token).SuppressCancellationThrow();
                if (isCanceled) return;
                continue;
            }

            if (CheckIfPlayerIsBehindWall(_audioDataObject, out RaycastHit tempHit))
                AssignNewCutoffFreqToCurrentSource(_filter, tempHit);

            else if (_filter != null)
                _filter.cutoffFrequency = defaultCuttoffFreqValue;

            bool canceledDuringWait = await UniTask.Delay(checkIntervalMs, delayType: DelayType.DeltaTime, cancellationToken: _token).SuppressCancellationThrow();
            if (canceledDuringWait) return;

            elapsedPlayTime += timeIntervalBetweenPositionChecks;
        }

        if (_audioDataObject != null) _audioDataObject.PoolIndex = -1;
    }
#endif

    private bool CheckIfPlayerIsBehindWall(AudioDataObject _audioDataObject, out RaycastHit hitinfo)
    {
        hitinfo = default;

        if (playerAudioListenerTransform == null || _audioDataObject == null || _audioDataObject.CallerTransform == null)
            return false;

        Vector3 startPos = _audioDataObject.CallerTransform.position;
        Vector3 direction = playerAudioListenerTransform.position - startPos;
        float maxDistance = direction.magnitude;

        if (Physics.Raycast(startPos, direction.normalized, out RaycastHit tempHit, maxDistance, automaticallyGeneratedWallLayerMask))
        {
            hitinfo = tempHit;
            return true;
        }
        return false;
    }

    private void AssignNewCutoffFreqToCurrentSource(AudioLowPassFilter _lowPassFilter, RaycastHit _hitinfo)
    {
        if (_lowPassFilter == null || dictionaryProvider.WallLayerMaskDictionary == null) return;

        if (dictionaryProvider.WallLayerMaskDictionary.TryGetValue(_hitinfo.transform.gameObject.layer, out float targetValue))
        {
            _lowPassFilter.cutoffFrequency = targetValue;
            Debug.Log($"Layer: {_hitinfo.transform.gameObject.layer} with Freq: {targetValue} was assigned (hard).");
        }
    }

    private void StopCurrentAudioSource(AudioDataObject _audioDataObject)
    {
        if (_audioDataObject == null || _audioDataObject.PoolIndex == -1 || !_audioDataObject.canHandleAudioSource)
            return;

        // DIE PERFORMANTE REVOLUTION: Kein Schleifen-Suchen mehr nötig!
        int targetIndex = _audioDataObject.PoolIndex;

        sourceProvider.allAudioSources[targetIndex].Source.Stop();

#if !USE_UNITASK
        // Blitzschnelles Beenden der Coroutine über den int-Key
        if (activeCoroutineChecks.TryGetValue(targetIndex, out Coroutine runningCoroutine))
        {
            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
            activeCoroutineChecks.Remove(targetIndex);
        }
#else
        sourceProvider.poolTokenSources[targetIndex].Cancel();
#endif
        _audioDataObject.PoolIndex = -1;
    }

    private void GenerateLayerMaskFromDictionary()
    {
        int combinedBitmask = 0;

        if (dictionaryProvider.WallLayerMaskDictionary != null)
        {
            foreach (int layerKey in dictionaryProvider.WallLayerMaskDictionary.Keys)
            {
                int layerBit = 1 << layerKey;
                combinedBitmask |= layerBit;
            }
        }

        automaticallyGeneratedWallLayerMask = combinedBitmask;
    }

    private void OnDestroy()
    {
#if !USE_UNITASK
        foreach (var runningCoroutine in activeCoroutineChecks.Values)
        {
            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
        }
        activeCoroutineChecks.Clear();
#else
        linkedMasterTokenSource.Cancel();
        linkedMasterTokenSource.Dispose();

        for (int i = 0; i < sourceProvider.poolTokenSources.Length; i++)
        {
            if (sourceProvider.poolTokenSources[i] != null) sourceProvider.poolTokenSources[i].Dispose();
        }
#endif
    }
}