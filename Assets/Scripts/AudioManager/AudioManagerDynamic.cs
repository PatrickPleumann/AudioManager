using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class AudioManagerDynamic : MonoBehaviour
{
    private AudioManagerDictionaryProvider dictionaryProvider = new AudioManagerDictionaryProvider();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    //number of audio sources has impact on cpu thread speed: above 100 is mostly unnecessary >> stay with 30 - 60 to have very low performance impact
    [SerializeField][Range(1, 100)] private int numbersOfAudioSources = 50;

    [SerializeField] private float defaultCuttoffFreqValue = 5000f;
    [SerializeField] AudioVolumesTransferObject transferObject;
    [SerializeField] private float numberOfAll3DAudioGameObjects;
    [SerializeField] private GameObject audioGameObjectPrefab;
    [SerializeField] private AudioClip audioClipPrefab;

    [SerializeField] private LayerMask walls;

    [SerializeField] private CutoffFreqLayerBehaviour[] CutOffFrequenciesPerLayerMask;

    [SerializeField] private Transform playerAudioListenerTransform;

    [SerializeField][Range(0, 1)] private float timeIntervalBetweenPositionChecks;


    private GameObject[] allAudioSources_Array;

    public static UnityAction<AudioDataObject> CallAudioSourceDispatcher;

    private RaycastHit hitinfo;

    private void Awake()
    {
        allAudioSources_Array = new GameObject[numbersOfAudioSources];
    }

    private void Start()
    {
        InstantiateAudioGameObjects_Array(numbersOfAudioSources);

        dictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues(CutOffFrequenciesPerLayerMask);
        dictionaryProvider.FillDictionaryWithKeysAndValues(transferObject);
        //FillDictionaryWithKeysAndValues();

        CallAudioSourceDispatcher += ProvideCallerWithTransformableAudioSource;
    }

    private void OnDisable()
    {
        CallAudioSourceDispatcher -= ProvideCallerWithTransformableAudioSource;
    }

    private void InstantiateAudioGameObjects_Array(int _numberOfAudioSources)
    {
        for (int i = 0; i < _numberOfAudioSources; i++)
        {
            var temp = Instantiate(audioGameObjectPrefab);
            temp.transform.SetParent(this.transform);
            allAudioSources_Array[i] = temp;
        }
    }

    private void FillDictionaryWithKeysAndValues()
    {
        if (transferObject.AudioVolumes == null)
        {
            Debug.LogWarning("Transfer Object (Array) is null");
            return;
        }
        if (transferObject.AudioVolumes.Length <= 0)
        {
            Debug.LogWarning("No Audio Volumes found in Transfer Object");
            return;
        }

        if (transferObject.AudioVolumes != null && transferObject.AudioVolumes.Length > 0)
            for (int i = 0; i < transferObject.AudioVolumes.Length; i++)
            {
                if (transferObject.AudioVolumes[i] != null)
                    dictionaryProvider.volumeDictionary.Add(transferObject.AudioVolumes[i].CurrentAudioType, transferObject.AudioVolumes[i].Volume);
                else
                    Debug.Log("Audio Volume Array position: " + i + " is null. Check if your AudioVolumes_TransferObject may has a empty spot");
            }
    }

    private GameObject GetAudioGameObject()
    {
        if (allAudioSources_Array != null && allAudioSources_Array.Length > 0)
        {
            for (int i = 0; i < allAudioSources_Array.Length; i++)
            {
                var temp = allAudioSources_Array[i].GetComponent<AudioSource>();
                if (temp.isPlaying == false)
                {
                    return allAudioSources_Array[i];
                }
                else
                    continue;
            }
        }

        Debug.LogWarning("Either Audio Game Object array is empty or null");
        return null;
    }

    private void ProvideCallerWithTransformableAudioSource(AudioDataObject _audioDataObject) //should be public if called from outside
    {
        var temp = GetAudioGameObject();
        if (temp != null)
        {
            var source = temp.GetComponent<AudioSource>();
            var filter = temp.GetComponent<AudioLowPassFilter>();

            source.clip = _audioDataObject.CurrentClips[Random.Range(0, _audioDataObject.CurrentClips.Length)];

            if (dictionaryProvider.volumeDictionary.TryGetValue(_audioDataObject.CurrentType, out float curVolume))
                source.volume = curVolume;

            if (CheckIfPlayerIsBehindWall(_audioDataObject, out RaycastHit tempHit) == true)
            {
                AssignNewCutoffFreqToCurrentSource(filter, tempHit);
                CheckPlayerStillBehindWallAsyncRoutine(cancellationTokenSource.Token, _audioDataObject, filter, source.clip.length);
            }

            if (_audioDataObject.SetCallerAsParent == true)
                temp.transform.SetParent(_audioDataObject.CallerTransform);
            else
                temp.transform.position = _audioDataObject.CallerTransform.position;

            source.Play();

            Debug.Log($"AudioSource >{_audioDataObject.CurrentType}< was played with >{curVolume}< volume, " +
            $" >{filter.cutoffFrequency}< Cutoff Frequency");

            filter.cutoffFrequency = defaultCuttoffFreqValue;
        }
    }

    private bool CheckIfPlayerIsBehindWall(AudioDataObject _audioDataObject, out RaycastHit hitinfo) // checks if player behind wall - only usable with layermask
    {
        if (Physics.Raycast(_audioDataObject.CallerTransform.position,
            (playerAudioListenerTransform.position - _audioDataObject.CallerTransform.position), out RaycastHit tempHit, 100f, walls) == true)
        {
            hitinfo = tempHit;
            return true;
        }
        hitinfo = default;
        return false;
    }

    private void AssignNewCutoffFreqToCurrentSource(AudioLowPassFilter _lowPassFilter, RaycastHit _hitinfo) // checks if player behind wall - only usable with layermask
    {
        dictionaryProvider.WallLayerMaskDictionary.TryGetValue(_hitinfo.transform.gameObject.layer, out float value);
        _lowPassFilter.cutoffFrequency = value;
    }

    private async void CheckPlayerStillBehindWallAsyncRoutine(CancellationToken _token, AudioDataObject _audioDataObject, AudioLowPassFilter _filter, float _currentClipLength)
    {
        _token.ThrowIfCancellationRequested();

        float currentClipLengthMilliseconds = _currentClipLength * 1000;
        int intervalCounter = (int)(_currentClipLength * timeIntervalBetweenPositionChecks);

        for (int i = 0; i < intervalCounter; i++)
        {
            await Task.Delay((int)(1000 * timeIntervalBetweenPositionChecks));

            if (CheckIfPlayerIsBehindWall(_audioDataObject, out RaycastHit tempHit) == true)
                AssignNewCutoffFreqToCurrentSource(_filter, tempHit);
            else
                _filter.cutoffFrequency = defaultCuttoffFreqValue;
        }
    }

    private void OnDestroy()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }
}
