using System.Collections.Generic;
using AudioFramework.Core;
using AudioFramework.Configuration;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Pure, Unity-independent step-4 transform: flattens the nested, inspector-friendly
    /// <see cref="DuckRule"/> list (grouped by trigger) into the flat <see cref="DuckPair"/> list the
    /// <see cref="DuckTargetPolicy"/> consumes — one pair per (trigger, target). A dumb transform: it does
    /// NOT filter self-targets or duplicates (the policy already owns self-skip and min-stacking), keeping a
    /// single source of truth for those rules. Fill style (clears and refills the caller's list) so step-6
    /// runtime can re-flatten on config change without allocating a new list each time.
    /// </summary>
    public static class DuckRuleFlattening
    {
        /// <summary>
        /// Clears <paramref name="results"/> and appends one <see cref="DuckPair"/> per target across all
        /// <paramref name="rules"/>, preserving rule then target order. A null rule list or a rule with null
        /// targets contributes nothing (no exception).
        /// </summary>
        public static void Flatten(IReadOnlyList<DuckRule> rules, List<DuckPair> results)
        {
            results.Clear();

            if (rules == null) return;

            for (int i = 0; i < rules.Count; i++)
            {
                DuckTarget[] targets = rules[i].Targets;
                if (targets == null) continue;

                AudioCategory trigger = rules[i].Trigger;

                for (int j = 0; j < targets.Length; j++)
                {
                    results.Add(new DuckPair(trigger, targets[j].Category, targets[j].DuckedVolume));
                }
            }
        }
    }
}
