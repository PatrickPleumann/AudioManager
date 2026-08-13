using System.Collections.Generic;

using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Writes the BASE gain of a category — the settings-slider end of the same dictionary
    /// <see cref="CategoryVolumeSource"/> reads live every frame. Together they are the whole runtime volume
    /// feature: this unit changes the value, the source picks it up on the next frame, and
    /// <see cref="AudioVolumeWriteService"/> puts it on the sounding slots.
    ///
    /// Clamps to [0, 1] at the API boundary, unlike <see cref="VolumeResolver"/>, which clamps as a safety net at
    /// the end of the chain. The reason is read-back honesty: a slider that writes 1.5 and reads 1.5 while the
    /// player hears 1.0 shows a value the system never played.
    ///
    /// An unconfigured category is CREATED rather than rejected — a settings menu is the wrong place to throw at
    /// a misconfiguration — and the creation is reported so the caller can warn about it.
    ///
    /// Deliberately writes ONLY the dictionary, never the AudioSystemConfig asset: writing the ScriptableObject
    /// would permanently rewrite the user's configuration from play mode.
    /// </summary>
    public class CategoryVolumeWriter
    {
        private readonly Dictionary<AudioCategory, float> volumeDictionary;

        public CategoryVolumeWriter(Dictionary<AudioCategory, float> _volumeDictionary)
        {
            volumeDictionary = _volumeDictionary;
        }

        /// <summary>
        /// Stores the requested gain for the given category, clamped to [0, 1].
        /// </summary>
        /// <param name="category">Category whose base gain is being set.</param>
        /// <param name="requested">Requested gain; values outside [0, 1] are clamped, not rejected.</param>
        /// <returns>Whether an existing entry was updated or a new one had to be created.</returns>
        public CategoryVolumeWriteOutcome Set(AudioCategory category, float requested)
        {
            bool hadEntry = volumeDictionary.ContainsKey(category);

            volumeDictionary[category] = ClampToGainRange(requested);

            return hadEntry ? CategoryVolumeWriteOutcome.Updated : CategoryVolumeWriteOutcome.EntryCreated;
        }

        private static float ClampToGainRange(float value)
        {
            if (value <= 0f) return 0f;
            if (value >= 1f) return 1f;

            return value;
        }
    }
}
