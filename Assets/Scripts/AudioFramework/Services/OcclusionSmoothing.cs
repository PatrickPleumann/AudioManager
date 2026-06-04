namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Pure, Unity-independent per-frame glide for the occlusion cutoff. Extracted so the smoothing curve is
    /// unit-testable without a play loop, and kept separate from <see cref="WallOcclusionMath"/> (which decides
    /// the TARGET cutoff) — this only moves the CURRENT cutoff toward that target over time.
    ///
    /// Model is MoveTowards: a constant rate of <paramref name="speed"/> Hz per second, frame-rate independent via
    /// <paramref name="deltaTime"/>. A non-positive <paramref name="speed"/> disables smoothing (instant snap),
    /// which restores the old hard-set behaviour.
    /// </summary>
    public static class OcclusionSmoothing
    {
        public static float Step(float current, float target, float deltaTime, float speed)
        {
            // speed <= 0 means "smoothing off": jump straight to the target (and dodge a pointless walk below).
            if (speed <= 0f) return target;

            float maxStep = speed * deltaTime;
            if (maxStep <= 0f) return current; // no time elapsed this frame -> no movement

            float diff = target - current;
            float absDiff = diff < 0f ? -diff : diff;

            // Within one step of the target -> settle exactly, never overshoot (which would cause jitter).
            if (absDiff <= maxStep) return target;

            return current + (diff > 0f ? maxStep : -maxStep);
        }
    }
}
