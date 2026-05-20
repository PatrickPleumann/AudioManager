#if !USE_UNITASK
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Data;
using AudioFramework.Configuration;
using AudioFramework.Ultilities;

namespace AudioFramework.Services.WallCheck
{
    public class AudioCoroutineWallCheckService_Test : IAudioWallCheckService_Test
    {
        private readonly AudioObject[] poolArray;
        private readonly AudioSystemConfigSO_Test config;
        private readonly Transform playerListener;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;
        private readonly MonoBehaviour routineRunner;

        private readonly Dictionary<int, Coroutine> activeCoroutineChecks = new Dictionary<int, Coroutine>();
        private readonly WaitForSeconds intervalWait;
        private readonly WaitForSeconds pauseWait;
        private int automaticallyGeneratedWallLayerMask;

        public AudioCoroutineWallCheckService_Test(
            AudioObject[] _poolArray,
            AudioSystemConfigSO_Test _config,
            Transform _playerListener,
            AudioManagerDictionaryProvider _dictionaryProvider,
            MonoBehaviour _routineRunner)
        {
            poolArray = _poolArray;
            config = _config;
            playerListener = _playerListener;
            dictionaryProvider = _dictionaryProvider;
            routineRunner = _routineRunner;

            intervalWait = new WaitForSeconds(config.timeIntervalBetweenPositionChecks);
            pauseWait = new WaitForSeconds(0.1f); // only for pausing intervalbased Routines...

            GenerateLayerMaskFromDictionary();
        }

        private void GenerateLayerMaskFromDictionary()
        {
            int combinedBitmask = 0;
            if (dictionaryProvider.WallLayerMaskDictionary != null)
            {
                foreach (int layerKey in dictionaryProvider.WallLayerMaskDictionary.Keys)
                {
                    combinedBitmask |= (1 << layerKey);
                }
            }
            automaticallyGeneratedWallLayerMask = combinedBitmask;
        }

        /// <summary>
        /// Raycast based position check, if the AudioListener position is behind a wall.
        /// </summary>
        /// <param name="originPos"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public bool CheckIfPlayerIsBehindWall(Vector3 originPos, out RaycastHit hitInfo)
        {
            hitInfo = default;
            if (playerListener == null) return false;
            Vector3 direction = playerListener.position - originPos;
            return Physics.Raycast(originPos, direction.normalized, out hitInfo, direction.magnitude, automaticallyGeneratedWallLayerMask);
        }

        /// <summary>
        /// Method starts the interval based WallCheck loop 
        /// </summary>
        /// <param name="audioDataObject">The object which holds all relevant audio clip data.</param>
        /// <param name="poolIndex">poolIndex refers to the AudioObject position in the array.</param>
        /// <param name="clipLength">Clip length is used for the amount of interval checks.</param>
        public void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex, float clipLength)
        {
            StopActiveCheck(poolIndex);
            Coroutine newCheck = routineRunner.StartCoroutine(CheckIfPlayerBehindWallRoutine(audioDataObject, poolIndex, clipLength));
            activeCoroutineChecks.Add(poolIndex, newCheck);
        }

        private IEnumerator CheckIfPlayerBehindWallRoutine(AudioDataObject audioDataObject, int poolIndex, float clipLength)
        {
            float elapsedPlayTime = 0f;
            AudioSource targetSource = poolArray[poolIndex].Source;
            AudioLowPassFilter filter = poolArray[poolIndex].Filter;

            while (elapsedPlayTime < clipLength)
            {
                if (audioDataObject == null || targetSource == null) yield break;

                bool isSoundPlaying = targetSource.isPlaying || Time.time < poolArray[poolIndex].BusyUntilTime;
                if (!isSoundPlaying)
                {
                    yield return pauseWait;
                    continue;
                }

                Vector3 currentPos = poolArray[poolIndex].GameObject.transform.position;
                if (CheckIfPlayerIsBehindWall(currentPos, out RaycastHit tempHit))
                {
                    if (dictionaryProvider.WallLayerMaskDictionary.TryGetValue(tempHit.transform.gameObject.layer, out float targetValue))
                        filter.cutoffFrequency = targetValue;
                }
                else if (filter != null)
                {
                    filter.cutoffFrequency = config.defaultCuttoffFreqValue;
                }

                yield return intervalWait;
                elapsedPlayTime += config.timeIntervalBetweenPositionChecks;
            }

            activeCoroutineChecks.Remove(poolIndex);
        }

        /// <summary>
        /// Based on the poolIndex fetches a probably still active Coroutine and stops it immedietly. This is a secure mechanism.
        /// </summary>
        /// <param name="poolIndex">The given index which not only refers to position in AudioObject[] but also to any other array-handled data.</param>
        public void StopActiveCheck(int poolIndex)
        {
            if (activeCoroutineChecks.TryGetValue(poolIndex, out Coroutine runningCoroutine))
            {
                if (runningCoroutine != null) routineRunner.StopCoroutine(runningCoroutine);
                activeCoroutineChecks.Remove(poolIndex);
            }
        }

        /// <summary>
        /// Stops all active Coroutines - called in OnDestroy();
        /// </summary>
        public void StopAllChecks()
        {
            foreach (var runningCoroutine in activeCoroutineChecks.Values)
            {
                if (runningCoroutine != null) routineRunner.StopCoroutine(runningCoroutine);
            }
            activeCoroutineChecks.Clear();
        }
    }
}
#endif
