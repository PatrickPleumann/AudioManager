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
    }
}
