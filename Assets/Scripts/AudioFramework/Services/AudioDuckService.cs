using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Core;
using AudioFramework.Interfaces;

namespace AudioFramework.Services.Mixing
{
    /// <summary>
    /// Stage-1 runtime glue and the SINGLE owner of <c>source.volume</c>. Driven once per frame from the manager's
    /// LateUpdate (after the fade tick), it resolves every playing slot's volume as
    /// <c>VolumeResolver.Resolve(base[category], fadeFactor[slot], duck[category])</c>:
    /// <list type="bullet">
    /// <item>base[category] is read LIVE from the volume dictionary, so a settings slider takes effect immediately;</item>
    /// <item>fadeFactor[slot] is owned by the fade service (it ramps the per-slot factor, no longer source.volume);</item>
    /// <item>duck[category] is kept by the <see cref="DuckFactorLedger"/>, which this service feeds each frame with
    /// the currently active categories and the flattened rules.</item>
    /// </list>
    /// With no <see cref="IDuckRuleProvider"/> registered the ledger is released with the rates it last ran on — a
    /// config that is gone can no longer supply them — so a category that was ducked glides out instead of snapping
    /// back. Volumes are still resolved either way, so the live slider works without a duck component. All per-frame
    /// collections are reused buffers — no allocation in the tick.
    /// </summary>
    public class AudioDuckService
    {
        private readonly AudioObject[] pool;
        private readonly Dictionary<AudioCategory, float> volumeDictionary;

        private IDuckRuleProvider provider;

        private readonly List<AudioCategory> activeCategories = new();
        private readonly List<DuckPair> flattenedPairs = new();
        private readonly DuckFactorLedger duckFactors = new();

        public AudioDuckService(AudioObject[] pool, Dictionary<AudioCategory, float> volumeDictionary)
        {
            this.pool = pool;
            this.volumeDictionary = volumeDictionary;
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
            UpdateDuckFactors(deltaTime);
            ApplyVolumes();
        }

        private void UpdateDuckFactors(float deltaTime)
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

        private void ApplyVolumes()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                AudioSource source = pool[i].Source;
                if (source == null || !source.isPlaying) continue;

                AudioCategory category = pool[i].Category;
                float basis = volumeDictionary.TryGetValue(category, out float configured) ? configured : 1f;
                float fade = pool[i].FadeFactor;
                float duck = duckFactors.FactorFor(category);

                source.volume = VolumeResolver.Resolve(basis, fade, duck);
            }
        }
    }
}
