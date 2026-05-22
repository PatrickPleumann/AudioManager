#define USE_UNITASK
#if USE_UNITASK
using System;
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
        private readonly AudioSystemConfigSO config;
        private readonly Transform playerListener;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;

        private readonly CancellationTokenSource[] poolTokenSources;
        private readonly CancellationTokenSource linkedMasterTokenSource;
        private int automaticallyGeneratedWallLayerMask;

        public AudioUniTaskWallCheckService(
            AudioObject[] _poolArray,
            AudioSystemConfigSO _config,
            Transform _playerListener,
            AudioManagerDictionaryProvider _dictionaryProvider)
        {
            poolArray = _poolArray;
            config = _config;
            playerListener = _playerListener;
            dictionaryProvider = _dictionaryProvider;

            poolTokenSources = new CancellationTokenSource[config.NumbersOfAudioSources];
            linkedMasterTokenSource = new CancellationTokenSource();

            for (int i = 0; i < poolTokenSources.Length; i++)
            {
                poolTokenSources[i] = new CancellationTokenSource();
            }

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

        public bool CheckIfPlayerIsBehindWall(Vector3 originPos, out RaycastHit hitInfo)
        {
            hitInfo = default;

            if (playerListener == false)
                return false;

            Vector3 direction = playerListener.position - originPos;

            return Physics.Raycast(originPos, direction.normalized, out hitInfo, direction.magnitude, automaticallyGeneratedWallLayerMask);
        }

        public void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex, float clipLength)
        {
            poolTokenSources[poolIndex].Cancel();
            poolTokenSources[poolIndex].Dispose();
            poolTokenSources[poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(linkedMasterTokenSource.Token);

            CheckIfPlayerBehindWallUniTaskVoid(poolTokenSources[poolIndex].Token, audioDataObject, poolIndex, clipLength).Forget();
        }

        private async UniTaskVoid CheckIfPlayerBehindWallUniTaskVoid(CancellationToken token, AudioDataObject audioDataObject, int poolIndex, float clipLength)
        {
            AudioSource targetSource = poolArray[poolIndex].Source;
            AudioLowPassFilter filter = poolArray[poolIndex].Filter;
            int checkIntervalMs = (int)(config.TimeIntervalBetweenPositionChecks * 1000);
            float elapsedPlayTime = 0f;

            while (elapsedPlayTime < clipLength)
            {
                if (token.IsCancellationRequested) return;
                if (audioDataObject == false || targetSource == false) return;

                bool isSoundPlaying = targetSource.isPlaying || Time.time < poolArray[poolIndex].BusyUntilTime;
                if (!isSoundPlaying)
                {
                    bool isCanceled = await UniTask.Delay(100, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
                    if (isCanceled) return;
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

                bool canceledDuringWait = await UniTask.Delay(checkIntervalMs, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
                if (canceledDuringWait)
                    return;

                elapsedPlayTime += config.TimeIntervalBetweenPositionChecks;
            }
        }

        public void StopActiveCheck(int poolIndex)
        {
            poolTokenSources[poolIndex].Cancel();
        }

        public void StopAllChecks()
        {
            linkedMasterTokenSource.Cancel();
            linkedMasterTokenSource.Dispose();

            for (int i = 0; i < poolTokenSources.Length; i++)
            {
                if (poolTokenSources[i] != null)
                    poolTokenSources[i].Dispose();
            }
        }
    }
}
#endif