using UnityEngine;

public class AudioPoolAcquisitionService_Test
{
    private readonly AudioObject[] _poolArray;
    private readonly AudioSystemConfigSO_Test _config;
    private readonly Transform _parentTransform;

    public AudioObject[] PoolArray => _poolArray;

    public AudioPoolAcquisitionService_Test(AudioSystemConfigSO_Test config, Transform parentTransform)
    {
        _config = config;
        _parentTransform = parentTransform;
        _poolArray = new AudioObject[_config.numbersOfAudioSources];

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < _poolArray.Length; i++)
        {
            var go = Object.Instantiate(_config.audioGameObjectPrefab);
            go.transform.SetParent(_parentTransform);

            _poolArray[i] = new AudioObject
            {
                GameObject = go,
                Source = go.GetComponent<AudioSource>(),
                Filter = go.GetComponent<AudioLowPassFilter>(),
                BusyUntilTime = 0f
            };
        }
    }

    public int GetFreePoolIndex()
    {
        float currentTime = Time.time;
        for (int i = 0; i < _poolArray.Length; i++)
        {
            if (!_poolArray[i].Source.isPlaying && currentTime >= _poolArray[i].BusyUntilTime)
            {
                return i;
            }
        }
        return -1;
    }

    public void SetSlotBusy(int poolIndex, float duration) => _poolArray[poolIndex].BusyUntilTime = Time.time + duration;
    public void ResetSlotBusy(int poolIndex) => _poolArray[poolIndex].BusyUntilTime = 0f;
}
