using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Abstraction over the one thing the volume write touches: a pool slot's category, whether it currently
    /// sounds, its per-slot fade factor, and the gain that gets written to it. Twin of
    /// <see cref="Fading.IFadeTarget"/> — keeping the write behind this interface lets the whole resolve-and-write
    /// orchestration be unit-tested in EditMode with a fake, without a real AudioSource or a play loop.
    ///
    /// Everything here is SLOT state. The category-wide factors (base gain, duck) are not on this interface: they
    /// are resolved per category by their own units and handed to the service separately.
    /// </summary>
    public interface IVolumeTarget
    {
        AudioCategory Category { get; }

        /// <summary>True while the slot actually sounds. A silent slot is skipped — its volume is left untouched.</summary>
        bool IsPlaying { get; }

        /// <summary>Per-slot fade factor in [0, 1], owned and ramped by the fade service (1 = no fade override).</summary>
        float FadeFactor { get; }

        float Volume { set; }
    }
}
