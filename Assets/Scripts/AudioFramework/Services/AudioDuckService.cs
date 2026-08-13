using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Interfaces;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Supplies ONE gain factor: the per-category duck. Driven once per frame from the manager's LateUpdate, it
    /// derives which categories are currently active, flattens the configured rules, and hands both to the
    /// <see cref="DuckFactorLedger"/>, which keeps and glides the factor per category. The resulting value is
    /// handed out through <see cref="ICategoryFactorSource"/> and combined with the other factors by
    /// <see cref="AudioVolumeWriteService"/> — this service never touches source.volume.
    ///
    /// With no <see cref="IDuckRuleProvider"/> registered the ledger is released with the rates it last ran on — a
    /// config that is gone can no longer supply them — so a category that was ducked glides out instead of
    /// snapping back. All per-frame collections are reused buffers — no allocation in the tick.
    /// </summary>
    public class AudioDuckService : ICategoryFactorSource
    {
        private readonly AudioObject[] pool;

        private IDuckRuleProvider provider;

        private readonly List<AudioCategory> activeCategories = new();
        private readonly List<DuckPair> flattenedPairs = new();
        private readonly DuckFactorLedger duckFactors = new();

        public AudioDuckService(AudioObject[] _pool)
        {
            pool = _pool;
        }

        /// <summary>Registers the passive duck config provider (called from the component's OnEnable).</summary>
        public void SetProvider(IDuckRuleProvider duckProvider) => provider = duckProvider;

        /// <summary>Clears the provider if it is the one registered (called from the component's OnDisable).</summary>
        public void ClearProvider(IDuckRuleProvider duckProvider)
        {
            if (provider == duckProvider) provider = null;
        }

        public void Tick(float deltaTime)
        {
            if (provider == null)
            {
                duckFactors.ReleaseAll(deltaTime);
                return;
            }

            DeriveActiveCategories();
            DuckRuleFlattening.Flatten(provider.Rules, flattenedPairs);

            duckFactors.Step(activeCategories, flattenedPairs, deltaTime, provider.AttackRate, provider.ReleaseRate);
        }

        /// <summary>Current duck factor of the category, 1.0 when it is not ducked.</summary>
        public float For(AudioCategory category) => duckFactors.FactorFor(category);

        /// <summary>Active = a slot that is playing and not paused. Deduplicated into the reused buffer.</summary>
        private void DeriveActiveCategories()
        {
            activeCategories.Clear();
            for (int i = 0; i < pool.Length; i++)
            {
                AudioSource source = pool[i].Source;
                if (source == null || !source.isPlaying || pool[i].IsPaused) continue;

                AudioCategory category = pool[i].Category;
                if (!activeCategories.Contains(category)) activeCategories.Add(category);
            }
        }
    }
}
