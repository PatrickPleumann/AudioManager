namespace AudioFramework.Configuration
{
    /// <summary>
    /// Pure, Unity-independent checks on the configured occlusion cutoff range. <see cref="Evaluate"/> answers whether
    /// the range leaves the wall check any room to work at all: both failure modes are silent in a running game — one
    /// inverts occlusion (open sounds muffled, walled sounds brighter), the other makes occlusion do nothing — so the
    /// inspector should say so while the user is still looking at the asset. <see cref="Advise"/> covers the softer
    /// case where the range works but will not sound the way an untouched configuration does.
    ///
    /// Lives next to the config rather than with the services so the configuration layer does not have to depend on
    /// the service layer for its own validation.
    /// </summary>
    public static class OcclusionRangeValidation
    {
        /// <summary>The open cutoff is treated as fully transparent from this frequency upward.</summary>
        public const float TransparentOpenCutoff = 20000f;

        /// <summary>Up to this frequency the floor leaves full muffling available.</summary>
        public const float UnobtrusiveFloor = 200f;

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

        /// <summary>
        /// Reports how a working range departs from an untouched configuration, so the inspector can say what the
        /// user will hear instead of telling them not to touch the fields.
        /// </summary>
        /// <param name="defaultCutoff">The open, un-occluded cutoff the wall check reduces down from.</param>
        /// <param name="minCutoff">The floor the wall check never reduces below.</param>
        /// <remarks>
        /// A range that <see cref="Evaluate"/> already rejects yields no advice: the warning about the broken range
        /// covers it, and stacking two more messages onto one mistake buries it.
        /// </remarks>
        public static OcclusionRangeAdvice Advise(float defaultCutoff, float minCutoff)
        {
            if (Evaluate(defaultCutoff, minCutoff) != OcclusionRangeIssue.None) return OcclusionRangeAdvice.None;

            OcclusionRangeAdvice advice = OcclusionRangeAdvice.None;

            if (defaultCutoff < TransparentOpenCutoff) advice |= OcclusionRangeAdvice.OpenCutoffNotTransparent;
            if (minCutoff > UnobtrusiveFloor) advice |= OcclusionRangeAdvice.FloorLimitsMuffling;

            return advice;
        }
    }
}
