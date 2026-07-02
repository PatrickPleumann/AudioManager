using System.Collections.Generic;
using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Pure, Unity-independent step-3 policy: resolves the per-category duck factor for a single target
    /// category from the set of currently active categories and the configured duck pairs. Strongest duck
    /// wins (<c>min</c> of the matching <see cref="DuckPair.DuckedVolume"/> factors); a category never ducks
    /// itself; no cascade (an active category triggers at full strength regardless of whether it is itself
    /// ducked — this policy only reads active-set membership). No active trigger for the target → 1.0.
    ///
    /// Extracted as pure logic so the stacking rule is unit-testable in EditMode without a play loop. The
    /// resulting factor feeds <see cref="DuckEnvelope"/> (per-frame glide) and <see cref="VolumeResolver"/>.
    /// </summary>
    public static class DuckTargetPolicy
    {
        /// <summary>
        /// Resolves the duck factor for <paramref name="target"/>: the minimum
        /// <see cref="DuckPair.DuckedVolume"/> over every pair that targets it, whose trigger differs from it
        /// (self-skip) and is in <paramref name="activeCategories"/>. No such pair → 1.0 (not ducked).
        /// </summary>
        public static float ResolveDuck(
            AudioCategory target,
            IReadOnlyCollection<AudioCategory> activeCategories,
            IReadOnlyList<DuckPair> pairs)
        {
            float result = 1.0f;

            for (int i = 0; i < pairs.Count; i++)
            {
                DuckPair pair = pairs[i];

                if (pair.Target != target) continue;
                if (pair.Trigger == target) continue;   // a category never ducks itself
                if (!IsActive(pair.Trigger, activeCategories)) continue;

                if (pair.DuckedVolume < result) result = pair.DuckedVolume;
            }

            return result;
        }

        private static bool IsActive(AudioCategory category, IReadOnlyCollection<AudioCategory> activeCategories)
        {
            foreach (AudioCategory active in activeCategories)
            {
                if (active == category) return true;
            }

            return false;
        }
    }
}
