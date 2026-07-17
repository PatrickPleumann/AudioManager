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
        private readonly IAudioListenerProvider listenerProvider;
        private readonly AudioManagerDictionaryProvider dictionaryProvider;

        private readonly CancellationTokenSource[] poolTokenSources;
        private readonly CancellationTokenSource linkedMasterTokenSource;
        private readonly int checkIntervalMs;
        private int automaticallyGeneratedWallLayerMask;
        private readonly RaycastHit[] wallHitBuffer = new RaycastHit[8]; // 8 is max Buffer size for RaycastNonAlloc buffer. Can be changed 

        public AudioUniTaskWallCheckService(
            AudioObject[] _poolArray,
            AudioSystemConfig _config,
            IAudioListenerProvider _listenerProvider,
            AudioManagerDictionaryProvider _dictionaryProvider)
        {
            poolArray = _poolArray;
            config = _config;
            listenerProvider = _listenerProvider;
            dictionaryProvider = _dictionaryProvider;
            checkIntervalMs = (int)(config.TimeIntervalBetweenPositionChecks * 1000);

            poolTokenSources = new CancellationTokenSource[config.NumberOfAudioSources];
            linkedMasterTokenSource = new CancellationTokenSource();

            for (int i = 0; i < poolTokenSources.Length; i++)
                poolTokenSources[i] = new CancellationTokenSource();

            GenerateLayerMaskFromDictionary();
        }

        private void GenerateLayerMaskFromDictionary()
            => automaticallyGeneratedWallLayerMask = WallLayerMask.FromLayers(dictionaryProvider.WallLayerMaskDictionary?.Keys);

        public void StartWallCheckLoop(AudioDataObject _audioDataObject, int _poolIndex)
        {
            poolTokenSources[_poolIndex].Cancel();
            poolTokenSources[_poolIndex].Dispose();
            poolTokenSources[_poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(linkedMasterTokenSource.Token);

            int startGeneration = poolArray[_poolIndex].Generation;
            WallCheckLoop(poolTokenSources[_poolIndex].Token, _audioDataObject, _poolIndex, startGeneration).Forget();
        }

        private async UniTaskVoid WallCheckLoop(CancellationToken _token, AudioDataObject _audioDataObject, int _poolIndex, int _startGeneration)
        {
            while (ShouldContinueLoop(_audioDataObject, _poolIndex, _startGeneration))
            {
                if (_token.IsCancellationRequested) return;
                if (_audioDataObject == false) return;

                if (IsCurrentlyActive(_poolIndex))
                    ApplyWallCheckFilter(_poolIndex);

                // R4: real-clock interval (UnscaledDeltaTime) so the wall-check keeps ticking at timeScale = 0 —
                // consistent with M1/M4. To actually pause occlusion the game calls PauseAll, not timeScale.
                bool canceled = await UniTask.Delay(checkIntervalMs, delayType: DelayType.UnscaledDeltaTime, cancellationToken: _token).SuppressCancellationThrow();
                if (canceled) return;
            }
        }

        private bool IsCurrentlyActive(int _poolIndex) =>
            poolArray[_poolIndex].Source.isPlaying || Time.unscaledTime < poolArray[_poolIndex].BusyUntilTime;

        private bool ShouldContinueLoop(AudioDataObject _audioDataObject, int _poolIndex, int _startGeneration)
        {
            if (poolArray[_poolIndex].Source == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AudioTool] Pooled audio source (slot {_poolIndex}) was destroyed externally — its wall-check was stopped. Do not destroy the internal 'Pooled Audio Source NNN' GameObjects; they belong to the pool.");
#endif
                return false;
            }
            return WallCheckContinuation.ShouldContinue(
                startGeneration: _startGeneration,
                currentGeneration: poolArray[_poolIndex].Generation,
                isPaused: poolArray[_poolIndex].IsPaused,
                isOneShot: _audioDataObject.IsOneShot,
                isPlaying: poolArray[_poolIndex].Source.isPlaying,
                currentTime: Time.unscaledTime,
                busyUntilTime: poolArray[_poolIndex].BusyUntilTime);
        }

        private void ApplyWallCheckFilter(int _poolIndex)
        {
            Vector3 currentPos = poolArray[_poolIndex].GameObject.transform.position;
            poolArray[_poolIndex].TargetCutoff = CalculateCutoffFrequency(currentPos);
        }

        private float CalculateCutoffFrequency(Vector3 _originPos)
        {
            if (!listenerProvider.TryGetPosition(out Vector3 listenerPos)) return config.DefaultCutoffFreqValue;

            Vector3 direction = listenerPos - _originPos;
            int hitCount = Physics.RaycastNonAlloc(_originPos, direction.normalized, wallHitBuffer, direction.magnitude, automaticallyGeneratedWallLayerMask);

            if (hitCount == 0) return config.DefaultCutoffFreqValue;

            float cutoff = config.DefaultCutoffFreqValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (dictionaryProvider.WallLayerMaskDictionary.TryGetValue(wallHitBuffer[i].transform.gameObject.layer, out float damping))
                    cutoff = WallOcclusionMath.ApplyWall(cutoff, config.MinCutoffFreqValue, damping);
            }

            return WallOcclusionMath.ClampToFloor(cutoff, config.MinCutoffFreqValue);
        }

        public void StopActiveCheck(int _poolIndex) => poolTokenSources[_poolIndex].Cancel();

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
