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
    /// <item>duck[category] is derived here: active categories → per-target duck factor (DuckTargetPolicy) → glided
    /// per frame (DuckEnvelope) toward that target.</item>
    /// </list>
    /// With no <see cref="IDuckRuleProvider"/> registered the duck scan is skipped entirely (every duck stays 1),
    /// but volumes are still resolved so the live slider works without a duck component. All per-frame collections
    /// are reused buffers — no allocation in the tick.
    /// </summary>
    public class AudioDuckService
    {
        private readonly AudioObject[] pool;
        private readonly Dictionary<AudioCategory, float> volumeDictionary;

        private IDuckRuleProvider provider;

        // Reused per-frame buffers (GC-free).
        private readonly List<AudioCategory> activeCategories = new();
        private readonly List<DuckPair> flattenedPairs = new();
        private readonly List<AudioCategory> duckTargets = new();
        private readonly Dictionary<AudioCategory, float> currentDuck = new();

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
                // No ducking configured: every category is un-ducked. Skip the whole scan.
                currentDuck.Clear();
                return;
            }

            DeriveActiveCategories();
            DuckRuleFlattening.Flatten(provider.Rules, flattenedPairs);
            CollectDuckTargets();

            float attackRate = provider.AttackRate;
            float releaseRate = provider.ReleaseRate;

            for (int i = 0; i < duckTargets.Count; i++)
            {
                AudioCategory target = duckTargets[i];
                float targetDuck = DuckTargetPolicy.ResolveDuck(target, activeCategories, flattenedPairs);
                float current = currentDuck.TryGetValue(target, out float existing) ? existing : 1f;
                currentDuck[target] = DuckEnvelope.Step(current, targetDuck, deltaTime, attackRate, releaseRate);
            }
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

        /// <summary>The distinct set of categories that any pair targets — the categories whose duck must be glided.</summary>
        private void CollectDuckTargets()
        {
            duckTargets.Clear();
            for (int i = 0; i < flattenedPairs.Count; i++)
            {
                AudioCategory target = flattenedPairs[i].Target;
                if (!duckTargets.Contains(target)) duckTargets.Add(target);
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
                float duck = currentDuck.TryGetValue(category, out float d) ? d : 1f;

                source.volume = VolumeResolver.Resolve(basis, fade, duck);
            }
        }
    }
}
