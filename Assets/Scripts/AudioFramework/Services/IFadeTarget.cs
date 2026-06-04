namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Abstraction over the one thing a fade touches: a volume it can read/write, and a stop it triggers when a
    /// fade-out completes. Keeping AudioFadeService behind this interface lets the whole fade orchestration run on
    /// pure index-based logic and be unit-tested in EditMode with a fake — no real AudioSource, no play loop.
    /// In production the real implementation wraps a pooled AudioSource and routes Stop() through the pool's
    /// existing stop path.
    /// </summary>
    public interface IFadeTarget
    {
        float Volume { get; set; }
        void Stop();

        /// <summary>
        /// External slot state the fade must RESPECT — it is NOT fade-internal. True while the slot is globally
        /// paused. A paused fade must be frozen (Tick may not advance it), so it resumes correctly on unpause instead
        /// of running its ramp through silence.
        /// </summary>
        bool IsPaused { get; }
    }
}
