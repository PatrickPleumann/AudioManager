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

                go.name = $"Pooled Audio Source {i:000}";

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

                if (!poolArray[i].Source.isPlaying && currentTime >= poolArray[i].BusyUntilTime && !poolArray[i].IsPaused)
                {

                    poolArray[i].Generation++;
                    return i;
                }
            }
            return -1;
        }

        public void SetSlotBusy(int poolIndex, float duration) => poolArray[poolIndex].BusyUntilTime = Time.time + duration;
        public void ResetSlotBusy(int poolIndex) => poolArray[poolIndex].BusyUntilTime = 0f;


        public void SetPausePolicy(int poolIndex, bool respectsGlobalPause)
        {
            poolArray[poolIndex].RespectsGlobalPause = respectsGlobalPause;
            poolArray[poolIndex].IsPaused = false;
        }


        public void ResetPauseState(int poolIndex) => poolArray[poolIndex].IsPaused = false;


        public void MarkSlotPaused(int poolIndex) => poolArray[poolIndex].IsPaused = true;


        public void SetFollowTarget(int poolIndex, Transform target)
        {
            poolArray[poolIndex].FollowTarget = target;
            poolArray[poolIndex].IsFollowing = target != null;
        }


        public void SetTargetCutoff(int poolIndex, float cutoff) => poolArray[poolIndex].TargetCutoff = cutoff;

        public int CurrentGeneration(int poolIndex) => poolArray[poolIndex].Generation;

        public bool IsHandleCurrent(AudioHandle handle)
        {
            if (handle.PoolIndex < 0 || handle.PoolIndex >= poolArray.Length) return false;
            return AudioHandleValidator.IsCurrent(handle.PoolIndex, handle.Generation, poolArray[handle.PoolIndex].Generation, poolArray.Length);
        }
    }
}
