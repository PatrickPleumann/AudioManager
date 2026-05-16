#define USE_UNITASK //TODO: REMOVE THIS
using System.Threading;
using UnityEngine;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif

public class AudioSourcePoolingService : MonoBehaviour 
{
    [SerializeField] private AudioManagerSettings settingsData;
    [Space]
    [SerializeField] private GameObject audioGameObjectPrefab;
    [Space]
    public AudioObject[] allAudioSources;

#if USE_UNITASK
    public CancellationTokenSource[] poolTokenSources;
#endif
    private void Awake()
    {
        allAudioSources = new AudioObject[settingsData.numberOfAudioObjects];

#if USE_UNITASK
        poolTokenSources = new CancellationTokenSource[settingsData.numberOfAudioObjects];
#endif

        InstantiateAudioGameObjects(settingsData.numberOfAudioObjects);
    }

    private void InstantiateAudioGameObjects(int _numberOfAudioSources)
    {
        for (int i = 0; i < _numberOfAudioSources; i++)
        {
            var tempGameObject = Instantiate(audioGameObjectPrefab);
            tempGameObject.transform.SetParent(this.transform);

            allAudioSources[i] = new AudioObject
            {
                GameObject = tempGameObject,
                Source = tempGameObject.GetComponent<AudioSource>(),
                Filter = tempGameObject.GetComponent<AudioLowPassFilter>()
            };

#if USE_UNITASK
            poolTokenSources[i] = new CancellationTokenSource();
#endif
        }
    }

    public int GetFreeAudioSourcePoolIndex()
    {
        for (int i = 0; i < allAudioSources.Length; i++)
        {
            if (!allAudioSources[i].Source.isPlaying) return i;
        }
        Debug.Log($"All {allAudioSources.Length} audio sources are occupied");
        return -1;
    }
}
