using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Pooling;
using AudioFramework.Interfaces;
using AudioFramework.Services.Fading;

namespace AudioFramework.Services.Following
{
    /// <summary>
    /// Keeps a spatial sound glued to a moving emitter Transform WITHOUT parenting the pooled AudioObject to it.
    /// Parenting would hand ownership of the pool slot to caller code: destroying the caller would destroy the
    /// pooled GameObject and permanently break the slot. Instead we copy the target's position every LateUpdate.
    /// If the target is destroyed mid-playback we stop the sound and free the slot — stopping (not playing on) is
    /// correctness, not courtesy: a follow sound is typically a loop, which would otherwise play forever at the
    /// death spot and leak its slot. GC-free per frame.
    /// </summary>
    public class AudioFollowService
    {
        private readonly AudioPoolAcquisitionService poolAcquisitionService;
        private readonly IAudioWallCheckService wallCheckService;
        private readonly AudioFadeService fadeService;

        public AudioFollowService(
            AudioPoolAcquisitionService _poolAcquisitionService,
            IAudioWallCheckService _wallCheckService,
            AudioFadeService _fadeService)
        {
            poolAcquisitionService = _poolAcquisitionService;
            wallCheckService = _wallCheckService;
            fadeService = _fadeService;
        }

        public void UpdateFollowers()
        {
            AudioObject[] pool = poolAcquisitionService.PoolArray;
            for (int i = 0; i < pool.Length; i++)
            {
                if (!pool[i].IsFollowing) continue;

                bool isActive = pool[i].Source != null && pool[i].Source.isPlaying;

                if (pool[i].FollowTarget == null)
                {
                    if (isActive)
                    {
                        pool[i].Source.Stop();
                        wallCheckService.StopActiveCheck(i);
#if UNITY_EDITOR
                        Debug.Log($"[AudioTool] A following sound stopped because its emitter was destroyed (pool slot {i}). " +
                            "On a scene change this is the expected course. If it happened mid-scene and you wanted the " +
                            "sound to outlive its emitter, play it without Follow Emitter, or hold a handle and stop it yourself.");
#endif
                    }
                    poolAcquisitionService.ResetSlotBusy(i);
                    poolAcquisitionService.SetFollowTarget(i, null);
                    poolAcquisitionService.ResetPauseState(i);
                   
                    fadeService.ClearFade(i);
                    continue;
                }

                if (isActive)
                    pool[i].GameObject.transform.position = pool[i].FollowTarget.position;
            }
        }
    }
}
