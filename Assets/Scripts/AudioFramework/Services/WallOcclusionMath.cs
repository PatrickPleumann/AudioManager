namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Pure, Unity-independent occlusion math shared by both wall-check services. Deliberately extracted
    /// so the per-wall step is unit-testable and — more importantly — lives in ONE place.
    ///
    /// <see cref="ApplyWall"/> is the model seam: a wall damps the running cutoff TOWARD the floor by its
    /// per-layer damping factor (0 = transparent, 1 = full damping to the floor in this one wall). The
    /// falloff is multiplicative — across walls the open range above the floor is scaled by ∏(1 − dᵢ), so
    /// it is order-independent and asymptotes to (never crosses) the floor. The calling loop, the floor
    /// clamp and the buffer iteration stay put, so it stays zero-GC. See the "Occlusion-Frequenzabfall"
    /// entry in CLAUDE.md.
    /// </summary>
    public static class WallOcclusionMath
    {
        /// <summary>
        /// One wall's contribution: damp the running cutoff toward <paramref name="minCutoff"/> by
        /// <paramref name="damping"/> (0 = wall is transparent, 1 = cutoff drops to the floor in this one wall).
        /// </summary>
        public static float ApplyWall(float currentCutoff, float minCutoff, float damping)
            => currentCutoff - (currentCutoff - minCutoff) * damping;

        /// <summary>The cutoff never drops below the configured floor, no matter how many walls accumulate.</summary>
        public static float ClampToFloor(float cutoff, float minCutoff)
            => cutoff < minCutoff ? minCutoff : cutoff;
    }
}
