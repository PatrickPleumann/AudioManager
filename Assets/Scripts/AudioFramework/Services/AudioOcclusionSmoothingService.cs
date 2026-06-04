using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Pooling;
using AudioFramework.Configuration;

namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Glides each wall-checked slot's low-pass cutoff toward the target the wall-check loop set, once per frame
    /// (driven by the manager's LateUpdate, like AudioFollowService). Decoupling the per-frame glide from the
    /// ~interval raycast is what removes the audible "pop" when a sound moves in or out of occlusion. The actual
    /// curve lives in the pure <see cref="OcclusionSmoothing"/>. GC-free per frame.
    /// </summary>
    public class AudioOcclusionSmoothingService
    {
        private readonly AudioPoolAcquisitionService poolAcquisitionService;
        private readonly AudioSystemConfig config;

        public AudioOcclusionSmoothingService(AudioPoolAcquisitionService _poolAcquisitionService, AudioSystemConfig _config)
        {
            poolAcquisitionService = _poolAcquisitionService;
            config = _config;
        }

        public void Tick(float deltaTime)
        {
            AudioObject[] pool = poolAcquisitionService.PoolArray;
            float speed = config.OcclusionSmoothingSpeed;

            for (int i = 0; i < pool.Length; i++)
            {
                AudioSource source = pool[i].Source;
                AudioLowPassFilter filter = pool[i].Filter;

                // Only wall-checked slots run the filter (W2 enables it solely for UseWallCheck sounds). The null
                // checks defend against a slot whose GameObject was destroyed externally (Unity == null on a dead one).
                if (source == null || filter == null || !filter.enabled) continue;

                // Skip idle slots, so a freed slot doesn't keep gliding. Mirrors the wall-check "is active" test.
                if (!source.isPlaying && Time.time >= pool[i].BusyUntilTime) continue;

                filter.cutoffFrequency = OcclusionSmoothing.Step(filter.cutoffFrequency, pool[i].TargetCutoff, deltaTime, speed);
            }
        }
    }
}
