using System.Collections.Generic;
using AudioFramework.Core;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Pure, Unity-independent ledger of the per-category duck factors over time (step 5 of the mixer/ducking
    /// feature). Owns the "which categories are currently ducked, and by how much" bookkeeping: each frame every
    /// tracked category is glided (<see cref="DuckEnvelope"/>) toward the factor <see cref="DuckTargetPolicy"/>
    /// resolves for it, and an entry is retired once it is fully recovered.
    ///
    /// A category is tracked while it is a configured duck target OR while it is still recovering — that second
    /// half is what lets a category glide back to full when its rule disappears at runtime, instead of freezing
    /// at its last ducked value. It also makes provider loss a plain case of the same path rather than a second
    /// code path: with no pairs, every tracked category simply resolves to 1.0 and releases.
    ///
    /// Extracted as pure logic so the bookkeeping is unit-testable in EditMode without a play loop. All per-frame
    /// collections are reused buffers — no allocation in the step.
    /// </summary>
    public class DuckFactorLedger
    {
        private static readonly AudioCategory[] NothingActive = System.Array.Empty<AudioCategory>();
        private static readonly DuckPair[] NothingConfigured = System.Array.Empty<DuckPair>();

        private readonly Dictionary<AudioCategory, float> factors = new();
        private readonly List<AudioCategory> trackedCategories = new();

        private float lastAttackRate;
        private float lastReleaseRate;

        /// <summary>
        /// Advances every tracked category by one frame toward its resolved duck factor, using
        /// <paramref name="attackRate"/> when ducking deeper and <paramref name="releaseRate"/> when recovering.
        /// <paramref name="activeCategories"/> is an <see cref="IReadOnlyList{T}"/> and not the wider
        /// <see cref="IReadOnlyCollection{T}"/> so that <see cref="DuckTargetPolicy"/> can iterate it by index
        /// without boxing an enumerator every frame — do not widen it back.
        /// </summary>
        public void Step(
            IReadOnlyList<AudioCategory> activeCategories,
            IReadOnlyList<DuckPair> pairs,
            float deltaTime,
            float attackRate,
            float releaseRate)
        {
            lastAttackRate = attackRate;
            lastReleaseRate = releaseRate;

            CollectTrackedCategories(pairs);

            for (int i = 0; i < trackedCategories.Count; i++)
            {
                AudioCategory category = trackedCategories[i];

                float target = DuckTargetPolicy.ResolveDuck(category, activeCategories, pairs);
                float current = factors.TryGetValue(category, out float existing) ? existing : 1f;
                float stepped = DuckEnvelope.Step(current, target, deltaTime, attackRate, releaseRate);

                if (stepped >= 1f) factors.Remove(category);
                else factors[category] = stepped;
            }
        }

        /// <summary>
        /// The categories to advance this frame: every configured duck target, PLUS every category still carrying
        /// a factor from an earlier frame. The second group is what releases a category whose rule disappeared.
        /// </summary>
        private void CollectTrackedCategories(IReadOnlyList<DuckPair> pairs)
        {
            trackedCategories.Clear();

            for (int i = 0; i < pairs.Count; i++)
            {
                AudioCategory target = pairs[i].Target;
                if (!trackedCategories.Contains(target)) trackedCategories.Add(target);
            }

            foreach (AudioCategory recovering in factors.Keys)
            {
                if (!trackedCategories.Contains(recovering)) trackedCategories.Add(recovering);
            }
        }

        /// <summary>
        /// Releases every tracked category toward full volume using the rates of the most recent
        /// <see cref="Step"/>. This is the path for a duck config that disappears at runtime: the rates disappear
        /// with it, so the ledger keeps the last ones it was driven with and glides out instead of snapping back.
        /// </summary>
        public void ReleaseAll(float deltaTime)
        {
            Step(NothingActive, NothingConfigured, deltaTime, lastAttackRate, lastReleaseRate);
        }

        /// <summary>Current duck factor for <paramref name="category"/>; 1.0 when it is not tracked (not ducked).</summary>
        public float FactorFor(AudioCategory category) => factors.TryGetValue(category, out float factor) ? factor : 1f;
    }
}
