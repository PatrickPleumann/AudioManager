#if USE_UNITASK
using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class AudioUniTaskWallCheckService_Test : IAudioWallCheckService_Test
{
    private readonly AudioObject[] _poolArray;
    private readonly AudioSystemConfigSO_Test _config;
    private readonly Transform _playerListener;
    private readonly AudioManagerDictionaryProvider _dictionaryProvider;

    private readonly CancellationTokenSource[] _poolTokenSources;
    private readonly CancellationTokenSource _linkedMasterTokenSource;
    private int _automaticallyGeneratedWallLayerMask;

    public AudioUniTaskWallCheckService_Test(
        AudioObject[] poolArray, 
        AudioSystemConfigSO_Test config, 
        Transform playerListener,
        AudioManagerDictionaryProvider dictionaryProvider)
    {
        _poolArray = poolArray;
        _config = config;
        _playerListener = playerListener;
        _dictionaryProvider = dictionaryProvider;

        _poolTokenSources = new CancellationTokenSource[_config.numbersOfAudioSources];
        _linkedMasterTokenSource = new CancellationTokenSource();

        for (int i = 0; i < _poolTokenSources.Length; i++)
        {
            _poolTokenSources[i] = new CancellationTokenSource();
        }

        GenerateLayerMaskFromDictionary();
    }

    private void GenerateLayerMaskFromDictionary()
    {
        int combinedBitmask = 0;
        if (_dictionaryProvider.WallLayerMaskDictionary != null)
        {
            foreach (int layerKey in _dictionaryProvider.WallLayerMaskDictionary.Keys)
            {
                combinedBitmask |= (1 << layerKey);
            }
        }
        _automaticallyGeneratedWallLayerMask = combinedBitmask;
    }

    public bool CheckIfPlayerIsBehindWall(Vector3 originPos, out RaycastHit hitInfo)
    {
        hitInfo = default;
        if (_playerListener == null) return false;
        Vector3 direction = _playerListener.position - originPos;
        return Physics.Raycast(originPos, direction.normalized, out hitInfo, direction.magnitude, _automaticallyGeneratedWallLayerMask);
    }

    public void StartWallCheckLoop(AudioDataObject audioDataObject, int poolIndex, float clipLength)
    {
        _poolTokenSources[poolIndex].Cancel();
        _poolTokenSources[poolIndex].Dispose();
        _poolTokenSources[poolIndex] = CancellationTokenSource.CreateLinkedTokenSource(_linkedMasterTokenSource.Token);

        CheckIfPlayerBehindWallUniTaskVoid(_poolTokenSources[poolIndex].Token, audioDataObject, poolIndex, clipLength).Forget();
    }

    private async UniTaskVoid CheckIfPlayerBehindWallUniTaskVoid(CancellationToken token, AudioDataObject audioDataObject, int poolIndex, float clipLength)
    {
        AudioSource targetSource = _poolArray[poolIndex].Source;
        AudioLowPassFilter filter = _poolArray[poolIndex].Filter;
        int checkIntervalMs = (int)(_config.timeIntervalBetweenPositionChecks * 1000);
        float elapsedPlayTime = 0f;

        while (elapsedPlayTime < clipLength)
        {
            if (token.IsCancellationRequested) return;
            if (audioDataObject == null || targetSource == null) return;

            bool isSoundPlaying = targetSource.isPlaying || Time.time < _poolArray[poolIndex].BusyUntilTime;
            if (!isSoundPlaying)
            {
                bool isCanceled = await UniTask.Delay(100, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled) return;
                continue;
            }

            Vector3 currentPos = _poolArray[poolIndex].GameObject.transform.position;
            if (CheckIfPlayerIsBehindWall(currentPos, out RaycastHit tempHit))
            {
                if (_dictionaryProvider.WallLayerMaskDictionary.TryGetValue(tempHit.transform.gameObject.layer, out float targetValue))
                    filter.cutoffFrequency = targetValue;
            }
            else if (filter != null)
            {
                filter.cutoffFrequency = _config.defaultCuttoffFreqValue;
            }

            bool canceledDuringWait = await UniTask.Delay(checkIntervalMs, delayType: DelayType.DeltaTime, cancellationToken: token).SuppressCancellationThrow();
            if (canceledDuringWait) return;

            elapsedPlayTime += _config.timeIntervalBetweenPositionChecks;
        }
    }

    public void StopActiveCheck(int poolIndex)
    {
        _poolTokenSources[poolIndex].Cancel();
    }

    public void StopAllChecks()
    {
        _linkedMasterTokenSource.Cancel();
        _linkedMasterTokenSource.Dispose();

        for (int i = 0; i < _poolTokenSources.Length; i++)
        {
            if (_poolTokenSources[i] != null) _poolTokenSources[i].Dispose();
        }
    }
}
#endif