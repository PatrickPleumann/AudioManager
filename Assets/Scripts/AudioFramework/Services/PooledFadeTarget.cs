using UnityEngine;
using AudioFramework.Core;
using AudioFramework.Services.Playback;

namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Real IFadeTarget for one pooled slot. In the two-gain-stage model the fade is NOT a direct source.volume
    /// writer: Volume maps to the slot's per-slot fade FACTOR (0..1), and the single owner of source.volume
    /// (AudioDuckService) combines that factor with the category base volume and duck each frame. Stop() routes
    /// through the shared StopSlot path, IsPaused exposes the slot's live pause state so a paused fade can freeze.
    /// Built once per slot at init (GC-free at runtime); the pool array is stable for the pool's whole lifetime.
    /// </summary>
    internal class PooledFadeTarget : IFadeTarget
    {
        private readonly AudioStopService stopService;
        private readonly AudioObject[] pool;
        private readonly int index;

        public PooledFadeTarget(AudioStopService stopService, AudioObject[] pool, int index)
        {
            this.stopService = stopService;
            this.pool = pool;
            this.index = index;
        }

        public float Volume
        {
            get => pool[index].FadeFactor;
            set => pool[index].FadeFactor = value;
        }

        public bool IsPaused => pool[index].IsPaused;

        public void Stop() => stopService.StopSlot(index);
    }
}
