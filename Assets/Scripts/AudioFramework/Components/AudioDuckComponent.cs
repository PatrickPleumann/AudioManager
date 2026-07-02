using System.Collections.Generic;
using UnityEngine;

using AudioFramework.Configuration;
using AudioFramework.Core;
using AudioFramework.Interfaces;

namespace AudioFramework.Components
{
    /// <summary>
    /// Optional, PASSIVE ducking config for the AudioManager. Sits on the SAME GameObject as
    /// <see cref="AudioManagerDynamic"/> and hands its rules to the manager's duck service via
    /// <see cref="IDuckRuleProvider"/> — it has no LateUpdate of its own and never touches source.volume. The
    /// manager keeps the single tick and the single volume writer; this component is just where a designer authors
    /// the duck matrix and the global attack/release. Leave it off entirely and ducking is simply disabled (every
    /// category plays un-ducked, with no per-frame cost).
    ///
    /// Lifecycle by design: registers in OnEnable, unregisters in OnDisable, and never touches the manager in
    /// Awake — so enable order does not matter.
    /// </summary>
    [RequireComponent(typeof(AudioManagerDynamic))]
    public class AudioDuckComponent : MonoBehaviour, IDuckRuleProvider
    {
        [Header("--- Duck Rules ---")]
        [Tooltip("Each rule: while its trigger category is active (a sound of that category is playing and not " +
                 "paused), every listed target category is ducked to its factor. Strongest (lowest) factor wins " +
                 "when several triggers duck the same target.")]
        [SerializeField] private DuckRule[] duckRules;

        [Header("--- Global Envelope ---")]
        [Tooltip("How fast a category ducks DEEPER when a trigger starts, in factor units per second " +
                 "(e.g. 5 = full-to-half in 0.1s). 0 = instant, no smoothing. One global value for the whole system.")]
        [SerializeField] private float attackRate = 5f;

        [Tooltip("How fast a category RECOVERS toward full when its triggers stop, in factor units per second " +
                 "(e.g. 2 = half-to-full in 0.25s). 0 = instant, no smoothing. One global value for the whole system.")]
        [SerializeField] private float releaseRate = 2f;

        [Header("--- Reserved (stage 2, not applied) ---")]
        [Tooltip("Reserved for future mixer routing. Not read yet — declared so the two-gain-stage split has a " +
                 "home for stage-2 config without a later structural change.")]
        [SerializeField] private CategoryMixerRoute[] reservedMixerRoutes;

        public IReadOnlyList<DuckRule> Rules => duckRules;
        public float AttackRate => attackRate;
        public float ReleaseRate => releaseRate;

        private void OnEnable()
        {
            GetComponent<AudioManagerDynamic>().RegisterDuckProvider(this);
        }

        private void OnDisable()
        {
            GetComponent<AudioManagerDynamic>().UnregisterDuckProvider(this);
        }
    }
}
