namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Pure, Unity-independent per-frame glide for the per-category duck factor. Twin of
    /// <see cref="AudioFramework.Services.WallCheck.OcclusionSmoothing"/>, but with an ASYMMETRIC rate: the
    /// duck factor (1 = not ducked, 0 = fully silenced — same factor the <see cref="VolumeResolver"/> consumes)
    /// glides toward its target via MoveTowards, using <paramref name="attackRate"/> when ducking DEEPER
    /// (factor falling) and <paramref name="releaseRate"/> when RECOVERING (factor rising).
    ///
    /// Extracted as pure logic so the envelope curve is unit-testable in EditMode without a play loop. Like the
    /// twin it does not clamp to [0, 1] — MoveTowards between two in-range values stays in range, and the final
    /// clamp lives in <see cref="VolumeResolver"/>.
    /// </summary>
    public static class DuckEnvelope
    {
        /// <summary>
        /// Glides <paramref name="current"/> toward <paramref name="target"/> by one frame's worth of the
        /// direction-selected rate (Hz-analogue: factor units per second), frame-rate independent via
        /// <paramref name="deltaTime"/>. A non-positive selected rate snaps instantly to the target (disables
        /// smoothing for that direction); a zero-length frame produces no movement.
        /// </summary>
        public static float Step(float current, float target, float deltaTime, float attackRate, float releaseRate)
        {
            float rate = target < current ? attackRate : releaseRate;

            if (rate <= 0f) return target;

            float maxStep = rate * deltaTime;
            if (maxStep <= 0f) return current; // no time elapsed this frame -> no movement

            float diff = target - current;
            float absDiff = diff < 0f ? -diff : diff;

            if (absDiff <= maxStep) return target;

            return current + (diff > 0f ? maxStep : -maxStep);
        }
    }
}
