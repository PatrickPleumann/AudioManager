using System.Collections.Generic;

using AudioFramework.Core;

namespace AudioFramework.Configuration
{
    /// <summary>
    /// Compares the category volumes a config actually lists against the categories that exist in
    /// <see cref="AudioCategory"/>, and reports the three ways that list can be wrong: a category with no
    /// entry (plays at full volume, silently), the same category twice (the first entry wins, the second is
    /// dead) and an entry at zero (the category is inaudible by configuration, not by accident).
    ///
    /// Pure and Unity-free so the decision can be unit tested and shared: the same set arithmetic answers
    /// "what is missing?" for the inspector and for a future setup button that fills the gaps.
    /// </summary>
    public static class CategoryVolumeCoverage
    {
        public readonly struct Result
        {
            public IReadOnlyList<AudioCategory> Missing { get; }
            public IReadOnlyList<AudioCategory> Duplicated { get; }
            public IReadOnlyList<AudioCategory> Silent { get; }

            public Result(IReadOnlyList<AudioCategory> missing, IReadOnlyList<AudioCategory> duplicated, IReadOnlyList<AudioCategory> silent)
            {
                Missing = missing;
                Duplicated = duplicated;
                Silent = silent;
            }
        }

        /// <summary>
        /// Evaluates one config's volume list against all defined categories. A null list is treated as an
        /// empty one — every category is then missing, which is exactly what the runtime does with it.
        /// </summary>
        public static Result Evaluate(IReadOnlyList<CategoryVolume> configured, IReadOnlyList<AudioCategory> allCategories)
        {
            List<AudioCategory> missing = new List<AudioCategory>();
            List<AudioCategory> duplicated = new List<AudioCategory>();
            List<AudioCategory> silent = new List<AudioCategory>();

            HashSet<AudioCategory> seen = new HashSet<AudioCategory>();

            if (configured != null)
            {
                for (int i = 0; i < configured.Count; i++)
                {
                    AudioCategory category = configured[i].Category;

                    if (!seen.Add(category))
                    {
                        if (!duplicated.Contains(category)) duplicated.Add(category);
                        continue;
                    }

                    if (configured[i].Volume <= 0f) silent.Add(category);
                }
            }

            for (int i = 0; i < allCategories.Count; i++)
                if (!seen.Contains(allCategories[i]))
                    missing.Add(allCategories[i]);

            return new Result(missing, duplicated, silent);
        }
    }
}
