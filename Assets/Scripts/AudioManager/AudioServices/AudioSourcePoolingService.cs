//#define USE_UNITASK //TODO: REMOVE THIS
using System.Threading;
using UnityEngine;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif

public class AudioSourcePoolingService : MonoBehaviour 
{
    [Space]
    [SerializeField] private int numberOfAudioObjects;
    [Space]
    [SerializeField] private GameObject audioGameObjectPrefab;
    [Space]
    public PoolAudioObject[] allAudioSources;

#if USE_UNITASK
    public CancellationTokenSource[] poolTokenSources;
#endif
    private void Awake()
    {
        allAudioSources = new PoolAudioObject[numberOfAudioObjects];

#if USE_UNITASK
        poolTokenSources = new CancellationTokenSource[numberOfAudioObjects];
#endif

        InstantiateAudioGameObjects(numberOfAudioObjects);
    }

    private void InstantiateAudioGameObjects(int _numberOfAudioSources)
    {
        for (int i = 0; i < _numberOfAudioSources; i++)
        {
            var tempGameObject = Instantiate(audioGameObjectPrefab);
            tempGameObject.transform.SetParent(this.transform);

            allAudioSources[i] = new PoolAudioObject
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
