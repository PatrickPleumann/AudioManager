using UnityEngine;
using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Real IVolumeTarget for one pooled slot — the place where the resolved stage-1 gain actually reaches Unity.
    /// Volume maps straight to <c>source.volume</c>; everything else on the interface is a read of the slot's own
    /// state. Built once per slot at init (GC-free at runtime); the pool array is stable for the pool's whole
    /// lifetime.
    /// <para>
    /// IsPlaying is the guard for the write: it reports false for a slot whose AudioSource is gone, and the write
    /// service skips such a slot entirely. Removing that skip, or widening IsPlaying, would let the setter run
    /// against a destroyed AudioSource.
    /// </para>
    /// </summary>
    internal class PooledVolumeTarget : IVolumeTarget
    {
        private readonly AudioObject[] pool;
        private readonly int index;

        public PooledVolumeTarget(AudioObject[] _pool, int _index)
        {
            pool = _pool;
            index = _index;
        }

        public AudioCategory Category => pool[index].Category;

        public bool IsPlaying
        {
            get
            {
                AudioSource source = pool[index].Source;
                return source != null && source.isPlaying;
            }
        }

        public float FadeFactor => pool[index].FadeFactor;

        public float Volume
        {
            set => pool[index].Source.volume = value;
        }
    }
}
