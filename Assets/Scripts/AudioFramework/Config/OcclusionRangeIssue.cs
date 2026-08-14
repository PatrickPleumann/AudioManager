namespace AudioFramework.Configuration
{
    /// <summary>
    /// The two ways the occlusion cutoff range can be configured so that the wall check cannot do its job. Kept as
    /// a value instead of a message string so the decision is unit-testable without pinning the wording — the text
    /// belongs to the Unity-facing validation, not to the rule.
    /// </summary>
    public enum OcclusionRangeIssue
    {
        /// <summary>The floor sits below the open cutoff — the wall check has room to work.</summary>
        None,

        /// <summary>
        /// The floor sits above the open cutoff, which inverts occlusion: an un-occluded sound plays at the low
        /// open value and sounds muffled, while a wall damps its cutoff UP toward the floor and brightens it.
        /// </summary>
        MinAboveDefault,

        /// <summary>
        /// Floor and open cutoff are identical: the wall check can never reduce anything, so occlusion is silently
        /// inert while looking configured.
        /// </summary>
        MinEqualsDefault
    }
}
