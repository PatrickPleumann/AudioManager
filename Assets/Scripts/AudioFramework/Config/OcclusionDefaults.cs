namespace AudioFramework.Configuration
{
    /// <summary>
    /// The cutoff range an untouched AudioSystemConfig ships with. Kept here rather than as literals on the asset so
    /// the two numbers exist exactly once: the serialized fields start from them, and the inspector quotes them back
    /// when it has to tell the user what a working range looks like.
    ///
    /// Unity-free on purpose — the inspector model states that it depends on no runtime types, and reaching into the
    /// ScriptableObject for these two values would quietly break that.
    /// </summary>
    public static class OcclusionDefaults
    {
        /// <summary>The open, un-occluded cutoff an untouched config starts from.</summary>
        public const float OpenCutoff = 22000f;

        /// <summary>The floor an untouched config never reduces below.</summary>
        public const float MinCutoff = 100f;
    }
}
