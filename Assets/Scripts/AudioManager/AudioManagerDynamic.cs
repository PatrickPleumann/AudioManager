//using System.Threading;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.Events;

//public class AudioManagerDynamic : MonoBehaviour
//{
//    private AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();
//    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

//    //number of audio sources has impact on cpu thread speed: above 100 is mostly unnecessary >> stay with 30 - 60 to have very low performance impact
//    [SerializeField][Range(1, 100)] private int numbersOfAudioSources = 50;

//    [SerializeField] private float defaultCuttoffFreqValue = 5000f;
//    [SerializeField] AudioVolumesTransferObject transferObject;
//    [SerializeField] private float numberOfAll3DAudioGameObjects;
//    [SerializeField] private GameObject audioGameObjectPrefab;
//    [SerializeField] private AudioClip audioClipPrefab;

//    //[SerializeField] private LayerMask walls;

//    [SerializeField] private CutoffFreqLayerBehaviour[] CutOffFrequenciesPerLayer;

//    [SerializeField] private Transform playerAudioListenerTransform;

//    [SerializeField][Range(0, 1)] private float timeIntervalBetweenPositionChecks;


//    private GameObject[] allAudioSources_Array;

//    public static UnityAction<AudioDataObject> CallAudioSourceDispatcher;
//    public static UnityAction<AudioDataObject> DynamicAudioSourceStop;

//    private RaycastHit hitinfo;

//    private void Awake()
//    {
//        allAudioSources_Array = new GameObject[numbersOfAudioSources];
//    }

//    private void Start()
//    {
//        InstantiateAudioGameObjects_Array(numbersOfAudioSources);

//        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(CutOffFrequenciesPerLayer);
//        dictionaryProvider.FillDictionaryWithKeysAndValues(transferObject);

//        CallAudioSourceDispatcher += ProvideCallerWithTransformableAudioSource;
//        DynamicAudioSourceStop += StopSourcePlaying;
//    }

//    private void OnDisable()
//    {
//        CallAudioSourceDispatcher -= ProvideCallerWithTransformableAudioSource;
//    }

//    private void InstantiateAudioGameObjects_Array(int _numberOfAudioSources)
//    {
//        for (int i = 0; i < _numberOfAudioSources; i++)
//        {
//            var temp = Instantiate(audioGameObjectPrefab);
//            temp.transform.SetParent(this.transform);
//            allAudioSources_Array[i] = temp;
//        }
//    }

//    private GameObject GetAudioGameObject()
//    {
//        if (allAudioSources_Array != null && allAudioSources_Array.Length > 0)
//        {
//            for (int i = 0; i < allAudioSources_Array.Length; i++)
//            {
//                var temp = allAudioSources_Array[i].GetComponent<AudioSource>();
//                if (temp.isPlaying == false)
//                {
//                    return allAudioSources_Array[i];
//                }
//                else
//                    continue;
//            }
//        }

//        Debug.LogWarning("Either Audio Game Object array is empty or null");
//        return null;
//    }

//    private void ProvideCallerWithTransformableAudioSource(AudioDataObject _audioDataObject) //should be public if called from outside
//    {
//        var temp = GetAudioGameObject();
//        if (temp != null)
//        {
//            var source = temp.GetComponent<AudioSource>();
//            var filter = temp.GetComponent<AudioLowPassFilter>();

//            //var objectToken = new CancellationToken();

//            _audioDataObject.CurrentBoundedAudioSource = source;

//            source.clip = _audioDataObject.CurrentClips[UnityEngine.Random.Range(0, _audioDataObject.CurrentClips.Length)];

//            if (dictionaryProvider.volumeDictionary.TryGetValue(_audioDataObject.CurrentType, out float curVolume))
//                source.volume = curVolume;

//            CheckIfPlayerBehindWallAsyncRoutine(cancellationTokenSource.Token, _audioDataObject, filter, source.clip.length);

//            if (_audioDataObject.SetCallerAsParent == true)
//                temp.transform.SetParent(_audioDataObject.CallerTransform);
//            else
//                temp.transform.position = _audioDataObject.CallerTransform.position;

//            source.Play();

//            Debug.Log($"AudioSource >{_audioDataObject.CurrentType}< was played with >{curVolume}< volume, " +
//            $" >{filter.cutoffFrequency}< Cutoff Frequency");

//            filter.cutoffFrequency = defaultCuttoffFreqValue;
//        }
//    }

//    private bool CheckIfPlayerIsBehindWall(AudioDataObject _audioDataObject, out RaycastHit hitinfo) // checks if player behind wall - only usable with layermask
//    {
//        if (Physics.Raycast(_audioDataObject.CallerTransform.position,
//            (playerAudioListenerTransform.position - _audioDataObject.CallerTransform.position), out RaycastHit tempHit, 100f) == true)
//        {
//            hitinfo = tempHit;
//            return true;
//        }
//        hitinfo = default;
//        return false;
//    }

//    private void AssignNewCutoffFreqToCurrentSource(CancellationToken _token, AudioLowPassFilter _lowPassFilter, RaycastHit _hitinfo) // checks if player behind wall - only usable with layermask
//    {
//        if (dictionaryProvider.WallLayerMaskDictionary == null)
//        {
//            Debug.Log("Layer-Dictionary is null");
//            return;
//        }

//        if (dictionaryProvider.WallLayerMaskDictionary.Count <= 0)
//        {
//            Debug.Log("Layer-Dictionarydoes not contain any values");
//            return;
//        }

//        if (_token.IsCancellationRequested)
//        {
//            Debug.Log("Operation cancelled!!!");
//            return;
//        }

//        dictionaryProvider.WallLayerMaskDictionary.TryGetValue(_hitinfo.transform.gameObject.layer, out float value);
//        _lowPassFilter.cutoffFrequency = value;
//        Debug.Log($"Layer: {_hitinfo.transform.gameObject.layer} with Freq: {value} was assigned");
//    }

//    private async Task CheckIfPlayerBehindWallAsyncRoutine(CancellationToken _token, AudioDataObject _audioDataObject, AudioLowPassFilter _filter, float _currentClipLength)
//    {

//        float currentClipLengthMilliseconds = _currentClipLength * 1000;
//        int intervalCounter = (int)(_currentClipLength / timeIntervalBetweenPositionChecks);

//        for (int i = 0; i < intervalCounter; i++)
//        {
//            if (_audioDataObject.CurrentBoundedAudioSource.isPlaying == false)
//                continue;
            
//            if (_token.IsCancellationRequested)
//                break;

//            if (CheckIfPlayerIsBehindWall(_audioDataObject, out RaycastHit tempHit) == true)
//                AssignNewCutoffFreqToCurrentSource(_token, _filter, tempHit);

//            else
//            {
//                _filter.cutoffFrequency = defaultCuttoffFreqValue;
//                Debug.Log($"Frequency back to default: {defaultCuttoffFreqValue}");
//            }
//            try
//            {
//                await Task.Delay((int)(1000 * timeIntervalBetweenPositionChecks));
//            }
//            catch (System.OperationCanceledException)
//            {
//                break;
//            }

//        }
//        _audioDataObject.CurrentBoundedAudioSource = null;
//    }

//    private void StopSourcePlaying(AudioDataObject _audioDataObject)
//    {
//        if (_audioDataObject == null)
//        {
//            Debug.Log("Audio Data Object is null");
//            return;
//        }

//        if (_audioDataObject.CurrentBoundedAudioSource == null)
//        {
//            Debug.Log("AudioDataObject: CurrentBoundedAudioSource is null");
//            return;
//        }
//        if (_audioDataObject.canHandleAudioSource == false)
//        {
//            Debug.Log("AudioDataObject: AudioSource handling is forbidden");
//            return;
//        }

//        _audioDataObject.CurrentBoundedAudioSource.Stop();
//    }

//    private void OnDestroy()
//    {
//        cancellationTokenSource.Cancel();
//        cancellationTokenSource.Dispose();
//    }
//}
