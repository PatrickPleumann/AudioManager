using UnityEngine;

using AudioFramework.Configuration;
using AudioFramework.Core;
using AudioFramework.Interfaces;

namespace AudioFramework.Pooling
{
    public class AudioPoolAcquisitionService  : IGetPoolIndex
    {
        private readonly AudioObject[] poolArray;
        private readonly AudioSystemConfig config;
        private readonly Transform parentTransform;

        public AudioObject[] PoolArray => poolArray;

        public AudioPoolAcquisitionService(AudioSystemConfig _config, Transform _parentTransform)
        {
            config = _config;
            parentTransform = _parentTransform;
            poolArray = new AudioObject[config.NumberOfAudioSources];

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolArray.Length; i++)
            {
                var go = Object.Instantiate(config.AudioGameObjectPrefab,parentTransform);

                poolArray[i] = new AudioObject
                {
                    GameObject = go,
                    Source = go.GetComponent<AudioSource>(),
                    Filter = go.GetComponent<AudioLowPassFilter>(),
                    BusyUntilTime = 0f
                };
            }
        }

        public int GetFreeAudioSourcePoolIndex()
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

        // Pass the emitter Transform to make this slot follow it, or null to stop following.
        public void SetFollowTarget(int poolIndex, Transform target)
        {
            poolArray[poolIndex].FollowTarget = target;
            poolArray[poolIndex].IsFollowing = target != null;
        }
    }
}
