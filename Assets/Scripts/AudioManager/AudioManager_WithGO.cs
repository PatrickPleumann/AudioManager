using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class AudioManager_WithGO : MonoBehaviour
{
    //number of audio sources has impact on cpu thread speed: above 100 is mostly unnecessary >> stay with 30 - 60 to have very low performance impact
    [SerializeField][Range(1, 100)] private int numbersOfAudioSources = 50;
    [SerializeField] AudioVolumes_TransferObject transferObject;
    [SerializeField] private float numberOfAll3DAudioGameObjects;
    [SerializeField] private GameObject audioGameObjectPrefab;
    [SerializeField] private AudioClip audioClipPrefab;

    public static AudioManager_WithGO Instance;

    private Dictionary<AudioTypeProvider, float> volumeDictionary; // stores the volume of each AudioType like Ambient, Music, SFX, etc...
    private GameObject[] allAudioSources_Array;
    private Queue<GameObject> allAudioSources_Queue;

    public static UnityAction<AudioDataObject> CallAudioSourceDispatcher;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }
        Instance = this;

        allAudioSources_Array = new GameObject[numbersOfAudioSources];
        allAudioSources_Queue = new Queue<GameObject>();
        volumeDictionary = new();


    }

    private void Start()
    {


        InstantiateAudioGameObjects_Array(numbersOfAudioSources);
        //InstantiateAudioGameObjects_Queue(numbersOfAudioSources);

        FillDictionaryWithKeysAndValues();

        CallAudioSourceDispatcher += ProvideCallerWithTransformableAudioSource;
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
    //private void InstantiateAudioGameObjects_Queue(int _numberOfAudioSources)
    //{
    //    for (int i = 0; i < _numberOfAudioSources; i++)
    //    {
    //        var temp = Instantiate(audioGameObjectPrefab);
    //        temp.transform.SetParent(this.transform);
    //        allAudioSources_Queue.Enqueue(temp);
    //    }
    //}

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
                volumeDictionary.Add(transferObject.AudioVolumes[i].CurrentAudioType, transferObject.AudioVolumes[i].Volume);
            }
    }

    //need to dev build profile of queue or array is more efficient in proving a free audio source 
    private GameObject GetAudioGameObject_Array()
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

    //need to dev build profile of queue or array is more efficient in proving a free audio source 
    //private GameObject GetAudioGameObject_Queue()
    //{
    //    if (allAudioSources_Queue != null && allAudioSources_Queue.Count > 0)
    //        return allAudioSources_Queue.Dequeue();

    //    Debug.Log("Either Audio Game Object queue is empty or null");
    //    return null;
    //}

    public void ProvideCallerWithTransformableAudioSource(AudioDataObject _audioDataObject) //should be public if called from outside
    {
        var temp = GetAudioGameObject_Array();
        if (temp != null)
        {
            var source = temp.GetComponent<AudioSource>();
            source.clip = _audioDataObject.CurrentClips[Random.Range(0, _audioDataObject.CurrentClips.Length)];

            if (volumeDictionary.TryGetValue(_audioDataObject.CurrentTypeProvider, out float curVolume))
                source.volume = curVolume;

            temp.transform.position = _audioDataObject.callerPosition;
            source.Play();
            Debug.Log($"AudioSource >>{_audioDataObject.CurrentTypeProvider}<< was played with >>{curVolume}<< volume");
        }
    }
    public void ProvideCallerWithTransformableAudioSource(AudioDataObject _audioDataObject, Transform _callerTransform, bool shouldBeParent) //should be public if called from outside
    {
        var temp = GetAudioGameObject_Array();

        if (temp != null)
        {
            var source = temp.GetComponent<AudioSource>();
            source.clip = _audioDataObject.CurrentClips[Random.Range(0, _audioDataObject.CurrentClips.Length)];

            if (volumeDictionary.TryGetValue(_audioDataObject.CurrentTypeProvider, out float curVolume))
                source.volume = curVolume;

            if (shouldBeParent == false)
                temp.transform.position = _callerTransform.position;
            else
                temp.transform.SetParent(_callerTransform);

            source.Play();
        }
    }

}
