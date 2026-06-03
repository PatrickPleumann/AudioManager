#if USE_UNITASK
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

using AudioFramework.Core;
using AudioFramework.Data;
using AudioFramework.Configuration;
using AudioFramework.Utilities;
using AudioFramework.Interfaces;

namespace AudioFramework.Services.WallCheck
{
    public class AudioUniTaskWallCheckService : IAudioWallCheckService
    {
        private readonly AudioObject[] poolArray;
        private readonly AudioSystemConfig config;
        private readonly Transform playerListener;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;

        private readonly CancellationTokenSource[] poolTokenSources;
        private readonly CancellationTokenSource linkedMasterTokenSource;
        private readonly int checkIntervalMs;
        private int automaticallyGeneratedWallLayerMask;
        private readonly RaycastHit[] wallHitBuffer = new RaycastHit[8];

        public AudioUniTaskWallCheckService(
            AudioObject[] _poolArray,
            AudioSystemConfig _config,
            Transform _playerListener,
            AudioManagerDictionaryProvider _dictionaryProvider)
        {
            poolArray = _poolArray;
            config = _config;
            playerListener = _playerListener;
            dictionaryProvider = _dictionaryProvider;
            checkIntervalMs = (int)(config.TimeIntervalBetweenPositionChecks * 1000);

            poolTokenSources = new CancellationTokenSource[config.NumbersOfAudioSources];
            linkedMasterTokenSource = new CancellationTokenSource();

            for (int i = 0; i < poolTokenSources.Length; i++)
                poolTokenSources[i] = new CancellationTokenSource();

            GenerateLayerMaskFromDictionary();
        }

        private void GenerateLayerMaskFromDictionary()
        {
            int combinedBitmask = 0;
            if (dictionaryProvider.WallLayerMaskDictionary != null)
                foreach (int layerKey in dictionaryProvider.WallLayerMaskDictionary.Keys)
                    combinedBitmask |= (1 << layerKey);
            automaticallyGeneratedWallLayerMask = combinedBitmask;
        }

        public void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex)
        {
            poolTokenSources[poolIndex].Cancel();
            poolTokenSources[poolIndex].Dispose();
            poolTokenSources[poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(linkedMasterTokenSource.Token);

            WallCheckLoop(poolTokenSources[poolIndex].Token, audioDataObject, poolIndex).Forget();
        }

        private async UniTaskVoid WallCheckLoop(CancellationToken token, AudioDataObject audioDataObject, int poolIndex)
        {
            while (ShouldContinueLoop(audioDataObject, poolIndex))
            {
                if (token.IsCancellationRequested) return;
                if (audioDataObject == false) return;

                if (IsCurrentlyActive(poolIndex))
                    ApplyWallCheckFilter(poolIndex);

                bool canceled = await UniTask.Delay(checkIntervalMs, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
                if (canceled) return;
            }
        }

        private bool IsCurrentlyActive(int poolIndex) =>
            poolArray[poolIndex].Source.isPlaying || Time.time < poolArray[poolIndex].BusyUntilTime;

        private bool ShouldContinueLoop(AudioDataObject audioDataObject, int poolIndex)
        {
            if (audioDataObject.IsOneShot)
                return poolArray[poolIndex].Source.isPlaying || Time.time < poolArray[poolIndex].BusyUntilTime;
            return poolArray[poolIndex].Source.isPlaying;
        }

        private void ApplyWallCheckFilter(int poolIndex)
        {
            Vector3 currentPos = poolArray[poolIndex].GameObject.transform.position;
            AudioLowPassFilter filter = poolArray[poolIndex].Filter;
            filter.cutoffFrequency = CalculateCutoffFrequency(currentPos);
        }

        private float CalculateCutoffFrequency(Vector3 originPos)
        {
            if (playerListener == false) return config.defaultCuttoffFreqValue;

            Vector3 direction = playerListener.position - originPos;
            int hitCount = Physics.RaycastNonAlloc(originPos, direction.normalized, wallHitBuffer, direction.magnitude, automaticallyGeneratedWallLayerMask);

            if (hitCount == 0) return config.defaultCuttoffFreqValue;

            float cutoff = config.defaultCuttoffFreqValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (dictionaryProvider.WallLayerMaskDictionary.TryGetValue(wallHitBuffer[i].transform.gameObject.layer, out float reduction))
                    cutoff -= reduction;
            }

            return Mathf.Max(cutoff, config.MinCutoffFreqValue);
        }

        public void StopActiveCheck(int poolIndex) => poolTokenSources[poolIndex].Cancel();

        public void StopAllChecks()
        {
            linkedMasterTokenSource.Cancel();
            linkedMasterTokenSource.Dispose();
            for (int i = 0; i < poolTokenSources.Length; i++)
                if (poolTokenSources[i] != null) poolTokenSources[i].Dispose();
        }
    }
}
#endif
