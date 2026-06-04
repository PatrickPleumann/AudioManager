namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Pure, Unity-independent occlusion math shared by both wall-check services. Deliberately extracted
    /// so the per-wall step is unit-testable and — more importantly — lives in ONE place.
    ///
    /// <see cref="ApplyWall"/> is the model seam: today it is LINEAR (a wall subtracts its layer reduction
    /// in Hz from the running cutoff). Switching to a logarithmic/multiplicative falloff later means
    /// changing only this method body; the calling loop, the floor clamp and the buffer iteration stay put,
    /// so it stays zero-GC. See the "Logarithmischer Frequenzabfall" entry in CLAUDE.md.
    /// </summary>
    public static class WallOcclusionMath
    {
        /// <summary>One wall's contribution: subtract its layer reduction (Hz) from the running cutoff.</summary>
        public static float ApplyWall(float currentCutoff, float layerReduction)
            => currentCutoff - layerReduction;

        /// <summary>The cutoff never drops below the configured floor, no matter how many walls accumulate.</summary>
        public static float ClampToFloor(float cutoff, float minCutoff)
            => cutoff < minCutoff ? minCutoff : cutoff;
    }
}
