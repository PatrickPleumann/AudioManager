using UnityEngine;
using AudioFramework.Services.Playback;

namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Real IFadeTarget for one pooled slot: maps Volume to the slot's AudioSource and routes Stop() through the
    /// shared StopSlot path. Built once per slot at init (GC-free at runtime); a pool slot's AudioSource reference is
    /// stable for the pool's whole lifetime, so caching it here is safe.
    /// </summary>
    internal class PooledFadeTarget : IFadeTarget
    {
        private readonly AudioSource source;
        private readonly AudioStopService stopService;
        private readonly int index;

        public PooledFadeTarget(AudioSource source, AudioStopService stopService, int index)
        {
            this.source = source;
            this.stopService = stopService;
            this.index = index;
        }

        public float Volume
        {
            get => source.volume;
            set => source.volume = value;
        }

        public void Stop() => stopService.StopSlot(index);
    }
}
