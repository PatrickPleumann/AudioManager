using System.Collections.Generic;

using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Resolves the BASE gain factor of a category — the value a settings slider writes. One of the independent
    /// factors that <see cref="VolumeResolver"/> combines; it neither knows about the other factors nor writes to
    /// any AudioSource, so the "who resolves what" split stays one unit per factor.
    ///
    /// The backing dictionary is read LIVE on every query, never snapshotted, so a slider change takes effect on
    /// the next frame instead of at the next dispatch. A category with no configured entry resolves to 1.0: an
    /// unconfigured sound plays at full volume rather than silently disappearing.
    ///
    /// Deliberately does NOT clamp — combining and clamping belong to <see cref="VolumeResolver"/>, and swallowing
    /// an out-of-range value here would hide a misconfiguration instead of letting the resolver handle it.
    /// </summary>
    public class CategoryVolumeSource : ICategoryFactorSource
    {
        private readonly Dictionary<AudioCategory, float> volumeDictionary;

        public CategoryVolumeSource(Dictionary<AudioCategory, float> _volumeDictionary)
        {
            volumeDictionary = _volumeDictionary;
        }

        /// <summary>Base gain of the given category, or 1.0 when the category has no configured entry.</summary>
        public float For(AudioCategory category)
        {
            return volumeDictionary.TryGetValue(category, out float configured) ? configured : 1f;
        }
    }
}
