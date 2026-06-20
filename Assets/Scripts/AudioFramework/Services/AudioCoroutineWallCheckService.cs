#if !USE_UNITASK
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Data;
using AudioFramework.Configuration;
using AudioFramework.Utilities;
using AudioFramework.Core;
using AudioFramework.Interfaces;

namespace AudioFramework.Services.WallCheck
{
    public class AudioCoroutineWallCheckService : IAudioWallCheckService
    {
        private readonly AudioObject[] poolArray;
        private readonly AudioSystemConfig config;
        private readonly Transform playerListener;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;
        private readonly MonoBehaviour routineRunner;

        private readonly Dictionary<int, Coroutine> activeCoroutineChecks = new Dictionary<int, Coroutine>();
        private readonly WaitForSeconds intervalWait;
        private readonly WaitForSeconds pauseWait;
        private int automaticallyGeneratedWallLayerMask;
        private readonly RaycastHit[] wallHitBuffer = new RaycastHit[8];

        public AudioCoroutineWallCheckService(
            AudioObject[] _poolArray,
            AudioSystemConfig _config,
            Transform _playerListener,
            AudioManagerDictionaryProvider _dictionaryProvider,
            MonoBehaviour _routineRunner)
        {
            poolArray = _poolArray;
            config = _config;
            playerListener = _playerListener;
            dictionaryProvider = _dictionaryProvider;
            routineRunner = _routineRunner;

            intervalWait = new WaitForSeconds(config.TimeIntervalBetweenPositionChecks);
            pauseWait = new WaitForSeconds(0.1f); // only for pausing intervalbased Routines...

            GenerateLayerMaskFromDictionary();
        }

        private void GenerateLayerMaskFromDictionary()
            => automaticallyGeneratedWallLayerMask = WallLayerMask.FromLayers(dictionaryProvider.WallLayerMaskDictionary?.Keys);

        /// <summary>
        /// Raycast based position check, if the AudioListener position is behind a wall.
        /// </summary>
        /// <param name="originPos"></param>
        /// <param name="hitInfo"></param>
        /// <returns></returns>
        public void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex)
        {
            StopActiveCheck(poolIndex);
            Coroutine newCheck = routineRunner.StartCoroutine(WallCheckLoop(audioDataObject, poolIndex));
            activeCoroutineChecks.Add(poolIndex, newCheck);
        }

        private IEnumerator WallCheckLoop(AudioDataObject audioDataObject, int poolIndex)
        {
            AudioSource targetSource = poolArray[poolIndex].Source;

            while (ShouldContinueLoop(audioDataObject, poolIndex))
            {
                if (audioDataObject == false || targetSource == false) yield break;

                if (IsCurrentlyActive(poolIndex))
                    ApplyWallCheckFilter(poolIndex);

                yield return intervalWait;
            }

            activeCoroutineChecks.Remove(poolIndex);
        }

        private bool IsCurrentlyActive(int poolIndex) =>
            poolArray[poolIndex].Source.isPlaying || Time.time < poolArray[poolIndex].BusyUntilTime;

        private bool ShouldContinueLoop(AudioDataObject audioDataObject, int poolIndex)
        {
            if (poolArray[poolIndex].Source == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AudioTool] Pooled audio source (slot {poolIndex}) was destroyed externally — its wall-check was stopped. Do not destroy the internal 'Pooled Audio Source NNN' GameObjects; they belong to the pool.");
#endif
                return false;
            }

       
            if (poolArray[poolIndex].IsPaused) return true;

            if (audioDataObject.IsOneShot)
                return poolArray[poolIndex].Source.isPlaying || Time.time < poolArray[poolIndex].BusyUntilTime;
            return poolArray[poolIndex].Source.isPlaying;
        }

        private void ApplyWallCheckFilter(int poolIndex)
        {
            Vector3 currentPos = poolArray[poolIndex].GameObject.transform.position;
            poolArray[poolIndex].TargetCutoff = CalculateCutoffFrequency(currentPos);
        }

        private float CalculateCutoffFrequency(Vector3 originPos)
        {
            if (playerListener == false) return config.DefaultCutoffFreqValue;

            Vector3 direction = playerListener.position - originPos;
            int hitCount = Physics.RaycastNonAlloc(originPos, direction.normalized, wallHitBuffer, direction.magnitude, automaticallyGeneratedWallLayerMask);

            if (hitCount == 0) return config.DefaultCutoffFreqValue;

            float cutoff = config.DefaultCutoffFreqValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (dictionaryProvider.WallLayerMaskDictionary.TryGetValue(wallHitBuffer[i].transform.gameObject.layer, out float damping))
                    cutoff = WallOcclusionMath.ApplyWall(cutoff, config.MinCutoffFreqValue, damping);
            }

            return WallOcclusionMath.ClampToFloor(cutoff, config.MinCutoffFreqValue);
        }

        /// <summary>
        /// Based on the poolIndex fetches a probably still active Coroutine and stops it immediately. This is a secure mechanism.
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
