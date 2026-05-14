#define USE_UNITASK //TODO: REMOVE THIS
using System.Threading;
using UnityEngine;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif

public class AudioSourceProviderService : MonoBehaviour 
{
    [SerializeField] private int numberOfAudioObjects;
    [SerializeField] private GameObject audioGameObjectPrefab;
    public CancellationTokenSource[] poolTokenSources;

    public PoolAudioObject[] allAudioSources;

    private void Awake()
    {
        allAudioSources = new PoolAudioObject[numberOfAudioObjects];
        poolTokenSources = new CancellationTokenSource[numberOfAudioObjects];

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

    public int GetFreeAudioPoolIndex()
    {
        for (int i = 0; i < allAudioSources.Length; i++)
        {
            if (!allAudioSources[i].Source.isPlaying) return i;
        }
        return -1;
    }
}
