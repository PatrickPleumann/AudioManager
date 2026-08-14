using System;

namespace AudioFramework.Configuration
{
    /// <summary>
    /// The ways a working occlusion cutoff range can still surprise the user. Unlike <see cref="OcclusionRangeIssue"/>
    /// these describe valid configurations — nothing is broken, the result just sounds different from what someone who
    /// never touched the two fields would expect. Both can apply at once, hence the flags.
    /// </summary>
    [Flags]
    public enum OcclusionRangeAdvice
    {
        /// <summary>Both ends sit where an untouched configuration would put them.</summary>
        None = 0,

        /// <summary>
        /// The open cutoff is low enough to be audible: a sound with nothing between it and the listener is already
        /// damped and sounds muffled.
        /// </summary>
        OpenCutoffNotTransparent = 1,

        /// <summary>
        /// The floor sits high enough to cap how muffled a sound can ever get: even full occlusion stays comparatively
        /// bright.
        /// </summary>
        FloorLimitsMuffling = 2
    }
}
