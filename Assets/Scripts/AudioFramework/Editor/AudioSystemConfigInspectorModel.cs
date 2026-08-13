using System.Collections.Generic;
using System.Text;

using AudioFramework.Configuration;

namespace AudioFramework.EditorTools
{
    /// <summary>
    /// Everything the config inspector needs, already resolved: counts, the coverage verdict and the flags.
    /// Flat and immutable so the wording and validation below stay free of Unity API calls.
    /// </summary>
    internal sealed class AudioSystemConfigSnapshot
    {
        internal string AssetName { get; set; }
        internal int PoolSize { get; set; }

        internal int ConfiguredVolumeCount { get; set; }
        internal int TotalCategoryCount { get; set; }
        internal IReadOnlyList<string> MissingCategories { get; set; }
        internal IReadOnlyList<string> DuplicatedCategories { get; set; }
        internal IReadOnlyList<string> SilentCategories { get; set; }

        internal float DefaultCutoff { get; set; }
        internal float MinCutoff { get; set; }
        internal float SmoothingSpeed { get; set; }
        internal float CheckInterval { get; set; }
        internal int WallLayerCount { get; set; }

        internal bool DuckingEnabled { get; set; }
        internal int DuckRuleCount { get; set; }

        internal bool HasPrefab { get; set; }

        internal bool CoversEveryCategory => MissingCategories.Count == 0;
    }

    /// <summary>
    /// Turns a config snapshot into the two pieces of prose the inspector shows: the plain-language summary
    /// of how this config will behave, and the list of findings worth reacting to. Deliberately free of
    /// UnityEditor and AudioFramework runtime types, so it can be unit tested.
    /// </summary>
    internal static class AudioSystemConfigInspectorModel
    {
        internal static string Describe(AudioSystemConfigSnapshot snapshot)
        {
            StringBuilder description = new StringBuilder();

            description.Append($"Pools {snapshot.PoolSize} audio sources");
            description.Append($", covers {snapshot.ConfiguredVolumeCount} of {snapshot.TotalCategoryCount} categories");
            description.Append(DescribeWallCheck(snapshot));
            description.Append(DescribeDucking(snapshot));
            description.Append('.');

            return description.ToString();
        }

        private static string DescribeWallCheck(AudioSystemConfigSnapshot snapshot)
        {
            if (snapshot.WallLayerCount == 0)
                return ", runs no wall check (no damping layers)";

            string layers = snapshot.WallLayerCount == 1 ? "1 layer" : $"{snapshot.WallLayerCount} layers";
            return $", checks walls on {layers} every {snapshot.CheckInterval:0.##} s";
        }

        private static string DescribeDucking(AudioSystemConfigSnapshot snapshot)
        {
            if (!snapshot.DuckingEnabled) return " and never ducks";

            string rules = snapshot.DuckRuleCount == 1 ? "1 duck rule" : $"{snapshot.DuckRuleCount} duck rules";
            return $" and applies {rules}";
        }

        internal static IReadOnlyList<InspectorFinding> Validate(AudioSystemConfigSnapshot snapshot)
        {
            List<InspectorFinding> findings = new List<InspectorFinding>();

            if (!snapshot.HasPrefab)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Error,
                    "No Audio GameObject Prefab assigned. The pool cannot be built and the manager disables itself."));

            if (snapshot.ConfiguredVolumeCount == 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                    "No category volumes configured. Every sound plays at full volume, and a settings slider has " +
                    "nothing to start from."));
            else if (snapshot.MissingCategories.Count > 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                    $"No volume entry for {Join(snapshot.MissingCategories)}. Sounds in these categories play at " +
                    "full volume and log a warning the first time a slider touches them."));

            if (snapshot.DuplicatedCategories.Count > 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                    $"{Join(snapshot.DuplicatedCategories)} listed more than once. The first entry wins at runtime " +
                    "and the others are ignored — keep exactly one entry per category."));

            if (snapshot.MinCutoff >= snapshot.DefaultCutoff)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                    "Min Cutoff Freq Value is not below Default Cutoff Freq Value, so a wall has no room to " +
                    "dampen anything — occlusion will be inaudible."));

            switch (DuckConfigValidation.Evaluate(snapshot.DuckingEnabled, snapshot.DuckRuleCount))
            {
                case DuckConfigIssue.EnabledWithoutRules:
                    findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                        "Ducking is on but no duck rules are configured. The per-frame scan for active categories " +
                        "runs without being able to duck anything."));
                    break;

                case DuckConfigIssue.RulesWithoutEnabled:
                    findings.Add(new InspectorFinding(InspectorFindingSeverity.Warning,
                        "Duck rules are configured but Ducking is off, so they are never read and nothing is ducked."));
                    break;
            }

            if (snapshot.SilentCategories.Count > 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Hint,
                    $"{Join(snapshot.SilentCategories)} set to 0 — sounds in these categories stay silent until a " +
                    "slider raises them."));

            if (snapshot.WallLayerCount == 0)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Hint,
                    "No wall damping layers defined, so sounds with Use Wall Check behave like unoccluded ones."));

            if (snapshot.SmoothingSpeed <= 0f)
                findings.Add(new InspectorFinding(InspectorFindingSeverity.Hint,
                    "Occlusion smoothing is 0, so the cutoff snaps instead of gliding — stepping out from behind a " +
                    "wall will pop."));

            return findings;
        }

        private static string Join(IReadOnlyList<string> names)
        {
            if (names.Count == 1) return $"'{names[0]}'";

            StringBuilder joined = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0) joined.Append(i == names.Count - 1 ? " and " : ", ");
                joined.Append($"'{names[i]}'");
            }

            return joined.ToString();
        }
    }
}
