namespace AudioFramework.Configuration
{
    /// <summary>
    /// Pure, Unity-independent check for whether the configured occlusion cutoff range leaves the wall check any
    /// room to work. Both failure modes are silent in a running game — one inverts occlusion (open sounds muffled,
    /// walled sounds brighter), the other makes occlusion do nothing at all — so the inspector should say so while
    /// the user is still looking at the asset.
    ///
    /// Lives next to the config rather than with the services so the configuration layer does not have to depend on
    /// the service layer for its own validation.
    /// </summary>
    public static class OcclusionRangeValidation
    {
        /// <summary>
        /// Reports how the cutoff floor and the open cutoff contradict each other, if they do.
        /// </summary>
        /// <param name="defaultCutoff">The open, un-occluded cutoff the wall check reduces down from.</param>
        /// <param name="minCutoff">The floor the wall check never reduces below.</param>
        /// <remarks>
        /// The equality test is deliberately exact, not an epsilon comparison: both values are typed into the
        /// inspector, so "the user entered the same number twice" is bit-identical. Loosening it to
        /// Mathf.Approximately would report a narrow but perfectly working range as the inert case.
        /// </remarks>
        public static OcclusionRangeIssue Evaluate(float defaultCutoff, float minCutoff)
        {
            if (minCutoff > defaultCutoff) return OcclusionRangeIssue.MinAboveDefault;
            if (minCutoff == defaultCutoff) return OcclusionRangeIssue.MinEqualsDefault;

            return OcclusionRangeIssue.None;
        }
    }
}
