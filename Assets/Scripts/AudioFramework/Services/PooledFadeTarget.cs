using UnityEngine;
using AudioFramework.Core;
using AudioFramework.Services.Playback;

namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Real IFadeTarget for one pooled slot: maps Volume to the slot's AudioSource, routes Stop() through the shared
    /// StopSlot path, and exposes the slot's live pause state so the fade service can freeze a paused fade. Built once
    /// per slot at init (GC-free at runtime); a pool slot's AudioSource and the pool array are stable for the pool's
    /// whole lifetime, so caching them here is safe.
    /// </summary>
    internal class PooledFadeTarget : IFadeTarget
    {
        private readonly AudioSource source;
        private readonly AudioStopService stopService;
        private readonly AudioObject[] pool;
        private readonly int index;

        public PooledFadeTarget(AudioSource source, AudioStopService stopService, AudioObject[] pool, int index)
        {
            this.source = source;
            this.stopService = stopService;
            this.pool = pool;
            this.index = index;
        }

        // Guarded against external destruction of the pooled slot: if someone deletes the "Pooled Audio Source"
        // GameObject mid-fade, the setter would otherwise throw a MissingReferenceException on the next Tick. We
        // degrade silently instead — the fade still completes by elapsed time and clears itself.
        public float Volume
        {
            get => source != null ? source.volume : 0f;
            set { if (source != null) source.volume = value; }
        }

        // Reads the live pause flag the pause service writes onto the slot; only the bool field is read, the struct
        // is not copied.
        public bool IsPaused => pool[index].IsPaused;

        public void Stop() => stopService.StopSlot(index);
    }
}
