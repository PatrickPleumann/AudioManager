namespace AudioFramework.Services.Playback
{
    /// <summary>
    /// The low-pass filter state a freshly dispatched pool slot should carry: whether the filter is
    /// active at all, and at which cutoff frequency it starts.
    /// </summary>
    public readonly struct LowPassDispatchState
    {
        public readonly bool Enabled;
        public readonly float CutoffFrequency;

        public LowPassDispatchState(bool enabled, float cutoffFrequency)
        {
            Enabled = enabled;
            CutoffFrequency = cutoffFrequency;
        }
    }

    /// <summary>
    /// Pure, Unity-independent decision for the low-pass filter state on dispatch. Extracted from the
    /// playback service so the occlusion-intent → filter-state rule is unit-testable without a play loop.
    ///
    /// Only wall-checked sounds need the filter: it stays enabled at the open cutoff so the wall-check
    /// loop can ride the cutoff down once a wall is between the source and the listener. Every other sound
    /// (2D music, UI, non-occluded SFX) bypasses the filter entirely — transparent sound and no DSP cost.
    /// </summary>
    public static class LowPassDispatchPolicy
    {
        /// <param name="useWallCheck">The ADO's occlusion intent (<c>AudioDataObject.UseWallCheck</c>).</param>
        /// <param name="defaultCutoffFrequency">The system's "open"/un-occluded cutoff (<c>AudioSystemConfig.DefaultCutoffFreqValue</c>).</param>
        public static LowPassDispatchState Resolve(bool useWallCheck, float defaultCutoffFrequency)
            => new LowPassDispatchState(useWallCheck, defaultCutoffFrequency);
    }
}
