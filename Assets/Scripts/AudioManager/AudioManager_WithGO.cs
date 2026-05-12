using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;


public class AudioManager_WithGO : MonoBehaviour
{
    //number of audio sources has impact on cpu thread speed: above 100 is mostly unnecessary >> stay with 30 - 60 to have very low performance impact
    [SerializeField][Range(1, 100)] private int numbersOfAudioSources = 50;

    [SerializeField] private float defaultCuttoffFreqValue = 5000f; 
    [SerializeField] AudioVolumes_TransferObject transferObject;
    [SerializeField] private float numberOfAll3DAudioGameObjects;
    [SerializeField] private GameObject audioGameObjectPrefab;
    [SerializeField] private AudioClip audioClipPrefab;

    [SerializeField] private LayerMask walls;


    [SerializeField] private Transform playerAudioListenerTransform;

    private Dictionary<AudioTypeProvider, float> volumeDictionary; // stores the volume of each AudioType like Ambient, Music, SFX, etc...
    private GameObject[] allAudioSources_Array;
    private Queue<GameObject> allAudioSources_Queue;

    public static UnityAction<AudioDataObject> CallAudioSourceDispatcher;


    private void Awake()
    {
        allAudioSources_Array = new GameObject[numbersOfAudioSources];
        allAudioSources_Queue = new Queue<GameObject>();
        volumeDictionary = new();
    }

    private void Start()
    {
        InstantiateAudioGameObjects_Array(numbersOfAudioSources);

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

    private void ProvideCallerWithTransformableAudioSource(AudioDataObject _audioDataObject) //should be public if called from outside
    {
        var temp = GetAudioGameObject_Array();
        if (temp != null)
        {
            var source = temp.GetComponent<AudioSource>();
            var filter = temp.GetComponent<AudioLowPassFilter>();
            source.clip = _audioDataObject.CurrentClips[Random.Range(0, _audioDataObject.CurrentClips.Length)];



            if (volumeDictionary.TryGetValue(_audioDataObject.CurrentTypeProvider, out float curVolume))
                source.volume = curVolume;

            if (CheckIfPlayerIsBehindWall(_audioDataObject) == true)
                filter.cutoffFrequency = 2000f;
            else
                filter.cutoffFrequency = defaultCuttoffFreqValue;


            if (_audioDataObject.SetCallerAsParent == true)
                temp.transform.SetParent(_audioDataObject.CallerTransform);
            else
                temp.transform.position = _audioDataObject.CallerTransform.position;

            source.Play();
            Debug.Log($"AudioSource >>{_audioDataObject.CurrentTypeProvider}<< was played with >>{curVolume}<< volume");
        }
    }

    private bool CheckIfPlayerIsBehindWall(AudioDataObject _audioDataObject) // checks if player behind wall - only usable with layermask
    {
        if (Physics.Raycast(_audioDataObject.CallerTransform.position,
            (playerAudioListenerTransform.position - _audioDataObject.CallerTransform.position), 100f, walls) == true)
        {
            return true;
        }
        return false;
    }
}
