#define USE_UNITASK
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif

public class AudioOcclusionService
{
    private readonly AudioManagerSettings audioManagerSettings;

    public AudioOcclusionService(AudioManagerSettings _audioManagerSettings)
    {
        audioManagerSettings = _audioManagerSettings;
    }

#if !USE_UNITASK
    [Header("Coroutine specific values")]
    public Dictionary<int, Coroutine> activeCoroutineChecks = new Dictionary<int, Coroutine>();
    public WaitForSeconds intervalWait;
    public WaitForSeconds pauseWait;
#else
    [Header("UniTask specific values")]
    public CancellationTokenSource linkedMasterTokenSource;
#endif


}
