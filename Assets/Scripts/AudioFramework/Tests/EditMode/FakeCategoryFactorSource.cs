using System.Collections.Generic;

using AudioFramework.Core;
using AudioFramework.Services.Mixing;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Test double for ICategoryFactorSource: returns exactly the configured factor per category. An unconfigured
    /// category returns 0 ON PURPOSE — a wrong category lookup then collapses the product to zero and fails
    /// loudly, instead of silently passing because the double happened to fall back to a neutral 1.0.
    /// </summary>
    internal class FakeCategoryFactorSource : ICategoryFactorSource
    {
        private readonly Dictionary<AudioCategory, float> factors = new();

        internal FakeCategoryFactorSource With(AudioCategory category, float factor)
        {
            factors[category] = factor;
            return this;
        }

        public float For(AudioCategory category) => factors.TryGetValue(category, out float factor) ? factor : 0f;
    }
}
