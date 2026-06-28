namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Pure, Unity-independent stage-1 gain resolver. Combines the three independent gain factors that own
    /// <c>source.volume</c> — category base volume (settings slider), per-slot fade factor, and per-category
    /// duck factor — into a single value, clamped to [0, 1].
    ///
    /// This is the SINGLE owner of how <c>source.volume</c> is computed (stage 1 of the two-gain-stage model).
    /// Everything downstream (mixer routing, effects, reverb, sends, snapshots) is stage 2 and does NOT depend
    /// on this math. Extracted as pure logic so the gain equation is unit-testable in EditMode without a play loop.
    /// </summary>
    public static class VolumeResolver
    {
        /// <summary>
        /// Resolves the stage-1 gain: <c>clamp01(basis · fade · duck)</c>.
        /// </summary>
        /// <param name="basis">Category base volume (settings slider).</param>
        /// <param name="fade">Per-slot fade factor in [0, 1] (1 = no fade override).</param>
        /// <param name="duck">Per-category duck factor in [0, 1] (1 = not ducked).</param>
        public static float Resolve(float basis, float fade, float duck)
        {
            float product = basis * fade * duck;

            if (product <= 0f) return 0f;
            if (product >= 1f) return 1f;

            return product;
        }
    }
}
