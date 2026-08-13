using System.Collections.Generic;
using AudioFramework.Configuration;

namespace AudioFramework.Interfaces
{
    /// <summary>
    /// Passive provider seam for the ducking config, mirrored on <see cref="AudioFramework.Interfaces"/> siblings
    /// like <c>IAudioWallCheckService</c>. The runtime <c>AudioDuckService</c> reads the configured duck rules and
    /// the global attack/release rates through this interface every frame — it never owns the config itself.
    /// Implemented by <c>AudioSystemConfig</c> and handed to the service once at construction; the seam stays so
    /// the rules can come from somewhere else without touching the service.
    ///
    /// Ducking is opt-in through the config's master switch: with it off the duck service is never created, so no
    /// rule is read and the per-frame scan for active categories does not run at all.
    /// </summary>
    public interface IDuckRuleProvider
    {
        /// <summary>The configured duck rules (grouped by trigger), flattened into pairs by the service each frame.</summary>
        IReadOnlyList<DuckRule> Rules { get; }

        /// <summary>Global glide rate (factor units per second) used while ducking DEEPER (factor falling). 0 = instant.</summary>
        float AttackRate { get; }

        /// <summary>Global glide rate (factor units per second) used while RECOVERING (factor rising). 0 = instant.</summary>
        float ReleaseRate { get; }
    }
}
