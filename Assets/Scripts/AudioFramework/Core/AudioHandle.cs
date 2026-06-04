namespace AudioFramework.Core
{
    public readonly struct AudioHandle
    {
        public readonly int PoolIndex;

        // Which occupation of that slot this handle refers to. The pool bumps a slot's generation on every reuse,
        // so a handle whose generation no longer matches the slot is stale (its sound already ended/was replaced) and
        // any Stop/Fade through it is silently ignored — it must never touch the foreign sound now on that slot.
        public readonly int Generation;

        // Internal: handles are OUTPUT-ONLY. Users receive them from Play*/Fade* and never construct their own, so a
        // bogus index can't enter the system (closes P6). Use AudioHandle.Invalid for the "no slot" result.
        internal AudioHandle(int index, int generation)
        {
            PoolIndex = index;
            Generation = generation;
        }

        /// <summary>The "no playable slot" result (pool full, or an ADO without CanHandleAudioSource).</summary>
        public static AudioHandle Invalid => new AudioHandle(-1, 0);

        /// <summary>Cheap structural check (no pool access): did this handle come from a successful Play/Fade call?
        /// Runtime currency (is the slot still this sound?) is a separate, pool-aware check.</summary>
        public bool IsValid => PoolIndex >= 0;
    }
}
