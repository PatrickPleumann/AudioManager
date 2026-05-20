//#define USE_UNITASK
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
//using Random = UnityEngine.Random;

//#if USE_UNITASK
//using Cysharp.Threading.Tasks;
//#endif

//public class AudioManagerDynamicMerged : MonoBehaviour
//{
//    // ==========================================
//    // ALLGEMEINE WERTE (Für beide Methoden)
//    // ==========================================
//    [Header("--- Allgemeine Einstellungen ---")]
//    [SerializeField][Range(1, 100)] private int numbersOfAudioSources = 50;
//    [SerializeField] private float defaultCuttoffFreqValue = 5000f;
//    [SerializeField] private AudioVolumesTransferObject transferObject;
//    [SerializeField] private GameObject audioGameObjectPrefab;
//    [SerializeField] private CutoffFreqLayerBehaviour[] CutOffFrequenciesPerLayer;
//    [SerializeField] private Transform playerAudioListenerTransform;
//    [SerializeField][Range(0.01f, 1f)] private float timeIntervalBetweenPositionChecks = 0.25f;

//    private AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();
//    private AudioObject[] allAudioSources_Array; // Nutzt dein globales Struct
//    private LayerMask _automaticallyGeneratedWallLayerMask;

//    // DIE ERWEITERTEN SYSTEM-DELEGATES MIT TICKET-RÜCKGABE
//    public static Func<AudioDataObject, AudioHandle> CallAudioSourceDispatcher;
//    public static Action<AudioHandle> DynamicAudioSourceStop;

//    // ==========================================
//    // METHODENSPEZIFISCHE WERTE
//    // ==========================================
//#if !USE_UNITASK
//    [Header("--- Coroutine Einstellungen ---")]
//    private readonly Dictionary<int, Coroutine> _activeCoroutineChecks = new Dictionary<int, Coroutine>();
//    private WaitForSeconds _intervalWait;
//    private WaitForSeconds _pauseWait;
//#else
//    [Header("--- UniTask Einstellungen ---")]
//    private CancellationTokenSource[] _poolTokenSources;
//    private CancellationTokenSource _linkedMasterTokenSource;
//#endif

//    private void Awake()
//    {
//        allAudioSources_Array = new AudioObject[numbersOfAudioSources];

//#if !USE_UNITASK
//        _intervalWait = new WaitForSeconds(timeIntervalBetweenPositionChecks);
//        _pauseWait = new WaitForSeconds(0.1f);
//        Debug.Log("[AudioTool] Initialisiert im REINEN COROUTINEN-Modus (Globales AudioObject).");
//#else
//        _poolTokenSources = new CancellationTokenSource[numbersOfAudioSources];
//        _linkedMasterTokenSource = new CancellationTokenSource();
//        Debug.Log("[AudioTool] Initialisiert im ALLOKATIONSFREIEN UNITASK-Modus (Globales AudioObject).");
//#endif
//    }

//    private void Start()
//    {
//        InstantiateAudioGameObjects_Array(numbersOfAudioSources);
//        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(CutOffFrequenciesPerLayer);
//        dictionaryProvider.FillDictionaryWithKeysAndValues(transferObject);

//        // Bitshift-LayerMask automatisch generieren
//        GenerateLayerMaskFromDictionary();

//        CallAudioSourceDispatcher += ProvideCallerWithTransformableAudioSource;
//        DynamicAudioSourceStop += StopSourcePlaying;
//    }

//    private void OnDisable()
//    {
//        CallAudioSourceDispatcher -= ProvideCallerWithTransformableAudioSource;
//        DynamicAudioSourceStop -= StopSourcePlaying;
//    }

//    private void InstantiateAudioGameObjects_Array(int _numberOfAudioSources)
//    {
//        for (int i = 0; i < _numberOfAudioSources; i++)
//        {
//            var go = Instantiate(audioGameObjectPrefab);
//            go.transform.SetParent(transform);


//            allAudioSources_Array[i] = new AudioObject
//            {
//                GameObject = go,
//                Source = go.GetComponent<AudioSource>(),
//                Filter = go.GetComponent<AudioLowPassFilter>(),
//                BusyUntilTime = 0f
//            };

//#if USE_UNITASK
//            _poolTokenSources[i] = new CancellationTokenSource();
//#endif
//        }
//    }

//    private int GetFreeAudioPoolIndex()
//    {
//        float currentTime = Time.time;
//        for (int i = 0; i < allAudioSources_Array.Length; i++)
//        {
//            if (!allAudioSources_Array[i].Source.isPlaying && currentTime >= allAudioSources_Array[i].BusyUntilTime)
//            {
//                return i;
//            }
//        }
//        return -1;
//    }

//    private AudioHandle ProvideCallerWithTransformableAudioSource(AudioDataObject _audioDataObject)
//    {
//        if (_audioDataObject == null) return new AudioHandle(-1);

//        int poolIndex = GetFreeAudioPoolIndex();
//        if (poolIndex == -1) return new AudioHandle(-1);

//        AudioObject poolObject = allAudioSources_Array[poolIndex];
//        AudioSource source = poolObject.Source;
//        AudioLowPassFilter filter = poolObject.Filter;

//        AudioClip chosenClip = _audioDataObject.CurrentClips[Random.Range(0, _audioDataObject.CurrentClips.Length)];
//        source.clip = chosenClip;

//        if (dictionaryProvider.volumeDictionary.TryGetValue(_audioDataObject.CurrentType, out float curVolume))
//            source.volume = curVolume;

//        if (_audioDataObject.SetCallerAsParent)
//            poolObject.GameObject.transform.SetParent(_audioDataObject.CallerTransform);
//        else
//            poolObject.GameObject.transform.position = _audioDataObject.CallerTransform.position;

//        filter.cutoffFrequency = defaultCuttoffFreqValue;

//        // --- DIE ONE-SHOT / LOOP WEICHE ---
//        if (_audioDataObject.IsOneShot)
//        {
//            allAudioSources_Array[poolIndex].BusyUntilTime = Time.time + chosenClip.length;
//            source.PlayOneShot(chosenClip);

//            // OPT-IN WEICHE: Wand-Check startet nur, wenn useWallCheck im ScriptableObject aktiv ist
//            if (_audioDataObject.UseWallCheck)
//            {
//#if !USE_UNITASK
//                StartWallCheckCoroutine(_audioDataObject, filter, chosenClip.length, poolIndex);
//#else
//                StartWallCheckUniTask(_audioDataObject, filter, chosenClip.length, poolIndex);
//#endif
//            }

//            return new AudioHandle(-1);
//        }
//        else
//        {
//            allAudioSources_Array[poolIndex].BusyUntilTime = 0f;

//            // OPT-IN WEICHE: Wand-Check startet nur, wenn useWallCheck im ScriptableObject aktiv ist
//            if (_audioDataObject.UseWallCheck)
//            {
//#if !USE_UNITASK
//                StartWallCheckCoroutine(_audioDataObject, filter, chosenClip.length, poolIndex);
//#else
//                StartWallCheckUniTask(_audioDataObject, filter, chosenClip.length, poolIndex);
//#endif
//            }

//            source.Play();
//            return new AudioHandle(poolIndex);
//        }
//    }

//    // ==========================================
//    // LOGIK-PFAD 1: REINE COROUTINEN 
//    // ==========================================
//#if !USE_UNITASK
//    private void StartWallCheckCoroutine(AudioDataObject audioDataObject, AudioLowPassFilter filter, float currentClipLength, int poolIndex)
//    {
//        if (_activeCoroutineChecks.TryGetValue(poolIndex, out Coroutine runningCoroutine))
//        {
//            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
//            _activeCoroutineChecks.Remove(poolIndex);
//        }

//        Coroutine newCheck = StartCoroutine(CheckIfPlayerBehindWallRoutine(audioDataObject, filter, currentClipLength, poolIndex));
//        _activeCoroutineChecks.Add(poolIndex, newCheck);
//    }

//    private IEnumerator CheckIfPlayerBehindWallRoutine(AudioDataObject audioDataObject, AudioLowPassFilter filter, float currentClipLength, int poolIndex)
//    {
//        float elapsedPlayTime = 0f;
//        AudioSource targetSource = allAudioSources_Array[poolIndex].Source;

//        if (CheckIfPlayerIsBehindWall(audioDataObject, out RaycastHit firstHit))
//            AssignNewCutoffFreqToCurrentSource(filter, firstHit, poolIndex);

//        while (elapsedPlayTime < currentClipLength)
//        {
//            if (audioDataObject == null || targetSource == null) yield break;

//            bool isSoundPlaying = targetSource.isPlaying || Time.time < allAudioSources_Array[poolIndex].BusyUntilTime;
//            if (!isSoundPlaying)
//            {
//                yield return _pauseWait;
//                continue;
//            }

//            if (CheckIfPlayerIsBehindWall(audioDataObject, out RaycastHit tempHit))
//                AssignNewCutoffFreqToCurrentSource(filter, tempHit, poolIndex);
//            else if (filter != null)
//                ResetCutoffFreqForSource(poolIndex);

//            yield return _intervalWait;
//            elapsedPlayTime += timeIntervalBetweenPositionChecks;
//        }

//        _activeCoroutineChecks.Remove(poolIndex);
//    }
//#endif

//    // ==========================================
//    // LOGIK-PFAD 2: HOCHLEISTUNGS-UNITASK 
//    // ==========================================
//#if USE_UNITASK
//    private void StartWallCheckUniTask(AudioDataObject audioDataObject, AudioLowPassFilter filter, float currentClipLength, int poolIndex)
//    {
//        _poolTokenSources[poolIndex].Cancel();
//        _poolTokenSources[poolIndex].Dispose();
//        _poolTokenSources[poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(_linkedMasterTokenSource.Token);

//        CheckIfPlayerBehindWallUniTaskVoid(
//            _poolTokenSources[poolIndex].Token, 
//            audioDataObject, 
//            filter, 
//            currentClipLength, 
//            poolIndex
//        ).Forget();
//    }

//    private async UniTaskVoid CheckIfPlayerBehindWallUniTaskVoid(CancellationToken token, AudioDataObject audioDataObject, AudioLowPassFilter filter, float currentClipLength, int poolIndex)
//    {
//        float elapsedPlayTime = 0f;
//        AudioSource targetSource = allAudioSources_Array[poolIndex].Source;
//        int checkIntervalMs = (int)(timeIntervalBetweenPositionChecks * 1000);

//        if (CheckIfPlayerIsBehindWall(audioDataObject, out RaycastHit firstHit))
//            AssignNewCutoffFreqToCurrentSource(filter, firstHit, poolIndex);

//        while (elapsedPlayTime < currentClipLength)
//        {
//            if (token.IsCancellationRequested) return;
//            if (audioDataObject == null || targetSource == null) return;

//            bool isSoundPlaying = targetSource.isPlaying || Time.time < allAudioSources_Array[poolIndex].BusyUntilTime;
//            if (!isSoundPlaying)
//            {
//                bool isCanceled = await UniTask.Delay(100, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
//                if (isCanceled) return;
//                continue;
//            }

//            if (CheckIfPlayerIsBehindWall(audioDataObject, out RaycastHit tempHit))
//                AssignNewCutoffFreqToCurrentSource(filter, tempHit, poolIndex);
//            else if (filter != null)
//                ResetCutoffFreqForSource(poolIndex);

//            bool canceledDuringWait = await UniTask.Delay(checkIntervalMs, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
//            if (canceledDuringWait) return;

//            elapsedPlayTime += timeIntervalBetweenPositionChecks;
//        }
//    }
//#endif

//    // ==========================================
//    // INTERNE AUTOMATISIERUNGEN (PHYSIK & BITSHIFT)
//    // ==========================================
//    private void GenerateLayerMaskFromDictionary()
//    {
//        int combinedBitmask = 0;
//        if (dictionaryProvider.WallLayerMaskDictionary != null)
//        {
//            foreach (int layerKey in dictionaryProvider.WallLayerMaskDictionary.Keys)
//            {
//                combinedBitmask |= (1 << layerKey);
//            }
//        }
//        _automaticallyGeneratedWallLayerMask = combinedBitmask;
//    }

//    private bool CheckIfPlayerIsBehindWall(AudioDataObject _audioDataObject, out RaycastHit hitinfo)
//    {
//        hitinfo = default;
//        if (playerAudioListenerTransform == null || _audioDataObject == null || _audioDataObject.CallerTransform == null)
//            return false;

//        Vector3 startPos = _audioDataObject.CallerTransform.position;
//        Vector3 direction = playerAudioListenerTransform.position - startPos;
//        float maxDistance = direction.magnitude;

//        if (Physics.Raycast(startPos, direction.normalized, out RaycastHit tempHit, maxDistance, _automaticallyGeneratedWallLayerMask))
//        {
//            hitinfo = tempHit;
//            return true;
//        }
//        return false;
//    }

//    private void AssignNewCutoffFreqToCurrentSource(AudioLowPassFilter _lowPassFilter, RaycastHit _hitinfo, int poolIndex)
//    {
//        if (_lowPassFilter == null || dictionaryProvider.WallLayerMaskDictionary == null) return;

//        if (dictionaryProvider.WallLayerMaskDictionary.TryGetValue(_hitinfo.transform.gameObject.layer, out float targetValue))
//        {
//            allAudioSources_Array[poolIndex].Filter.cutoffFrequency = targetValue;
//            Debug.Log($"[O(1)] Slot {poolIndex} wurde die Dämpfungsfrequenz {targetValue} hart zugewiesen.");
//        }
//    }

//    private void ResetCutoffFreqForSource(int poolIndex)
//    {
//        allAudioSources_Array[poolIndex].Filter.cutoffFrequency = defaultCuttoffFreqValue;
//    }

//    // ==========================================
//    // EXTERNE GLOBAL-STEUERUNGEN (STOP / PAUSE)
//    // ==========================================
//    private void StopSourcePlaying(AudioHandle handle)
//    {
//        if (!handle.IsValid) return;

//        int targetIndex = handle.PoolIndex;
//        AudioSource source = allAudioSources_Array[targetIndex].Source;

//        if (source != null) source.Stop();
//        allAudioSources_Array[targetIndex].BusyUntilTime = 0f;

//#if !USE_UNITASK
//        if (_activeCoroutineChecks.TryGetValue(targetIndex, out Coroutine runningCoroutine))
//        {
//            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
//            _activeCoroutineChecks.Remove(targetIndex);
//        }
//#else
//        _poolTokenSources[targetIndex].Cancel();
//#endif
//    }

//    public void PauseAllSources()
//    {
//        if (allAudioSources_Array == null) return;
//        for (int i = 0; i < allAudioSources_Array.Length; i++)
//        {
//            if (allAudioSources_Array[i].Source != null && allAudioSources_Array[i].Source.isPlaying)
//            {
//                allAudioSources_Array[i].Source.Pause();
//            }
//        }
//    }

//    public void UnpauseAllSources()
//    {
//        if (allAudioSources_Array == null) return;
//        for (int i = 0; i < allAudioSources_Array.Length; i++)
//        {
//            AudioSource source = allAudioSources_Array[i].Source;
//            if (source != null && source.clip != null)
//            {
//                source.UnPause();
//            }
//        }
//    }

//    private void OnDestroy()
//    {
//#if !USE_UNITASK
//        foreach (var runningCoroutine in _activeCoroutineChecks.Values)
//        {
//            if (runningCoroutine != null) StopCoroutine(runningCoroutine);
//        }
//        _activeCoroutineChecks.Clear();
//#else
//        _linkedMasterTokenSource.Cancel();
//        _linkedMasterTokenSource.Dispose();

//        for (int i = 0; i < _poolTokenSources.Length; i++)
//        {
//            if (_poolTokenSources[i] != null) _poolTokenSources[i].Dispose();
//        }
//#endif
//    }
//}