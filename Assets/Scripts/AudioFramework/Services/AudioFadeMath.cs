namespace AudioFramework.Services.Fading
{
    /// <summary>
    /// Pure, Unity-independent fade curve math. Deliberately extracted from the fade service so the
    /// trickiest part — the volume curve over time — is unit-testable in EditMode without a play loop.
    /// </summary>
    public static class AudioFadeMath
    {
        /// <summary>
        /// Returns the volume at a point in a linear fade from <paramref name="from"/> to <paramref name="to"/>.
        /// Clamps outside the [0, duration] window; a non-positive duration returns <paramref name="to"/> instantly.
        /// </summary>
        public static float Evaluate(float from, float to, float elapsed, float duration)
        {
            if (duration <= 0f)
                return to;

            float t = elapsed / duration;

            if (t <= 0f) return from;
            if (t >= 1f) return to;

            return from + (to - from) * t;
        }
    }
}
