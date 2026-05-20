using UnityEngine;

using AudioFramework.Configuration;

namespace AudioFramework.Pooling
{
    public class AudioPoolAcquisitionService_Test
    {
        private readonly AudioObject[] poolArray;
        private readonly AudioSystemConfigSO_Test config;
        private readonly Transform parentTransform;

        public AudioObject[] PoolArray => poolArray;

        public AudioPoolAcquisitionService_Test(AudioSystemConfigSO_Test _config, Transform _parentTransform)
        {
            this.config = _config;
            this.parentTransform = _parentTransform;
            poolArray = new AudioObject[this.config.numbersOfAudioSources];

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolArray.Length; i++)
            {
                var go = Object.Instantiate(config.audioGameObjectPrefab);
                go.transform.SetParent(parentTransform);

                poolArray[i] = new AudioObject
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
            for (int i = 0; i < poolArray.Length; i++)
            {
                if (!poolArray[i].Source.isPlaying && currentTime >= poolArray[i].BusyUntilTime)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetSlotBusy(int poolIndex, float duration) => poolArray[poolIndex].BusyUntilTime = Time.time + duration;
        public void ResetSlotBusy(int poolIndex) => poolArray[poolIndex].BusyUntilTime = 0f;
    }
}
