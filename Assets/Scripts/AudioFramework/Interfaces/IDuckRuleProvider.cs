using System.Collections.Generic;
using AudioFramework.Configuration;

namespace AudioFramework.Interfaces
{
    /// <summary>
    /// Passive provider seam for the ducking config, mirrored on <see cref="AudioFramework.Interfaces"/> siblings
    /// like <c>IAudioWallCheckService</c>. The runtime <c>AudioDuckService</c> reads the configured duck rules and
    /// the global attack/release rates through this interface every frame — it never owns the config itself. When
    /// no provider is registered the service treats every category as un-ducked (factor 1) and skips the duck scan
    /// entirely, so ducking is fully opt-in with no per-frame cost.
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
