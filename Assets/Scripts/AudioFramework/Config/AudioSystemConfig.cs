using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

using AudioFramework.Core;
using AudioFramework.Data;
using AudioFramework.Interfaces;
namespace AudioFramework.Configuration
{
    /// <summary>
    /// Central system configuration, and the single home of the ducking setup. Being an asset rather than a scene
    /// component is what makes the duck rules survive a scene change: the manager persists across loads, so any
    /// configuration living on a scene GameObject would silently be the one from whichever scene happened to load
    /// first. Implements <see cref="IDuckRuleProvider"/> directly, so the duck service reads its rules through the
    /// same seam it always did.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSystemConfig", menuName = "Audio Tool/System Config")]
    public class AudioSystemConfig : ScriptableObject, IDuckRuleProvider
    {
        [Header(" --- General values --- ")]
        [Tooltip("--Performance Tooltip-- : The total amount of audio source objects which are instantiated beforehand. " +
                 " More objects = lower performance.")]
        [FormerlySerializedAs("NumbersOfAudioSources")]
        [Range(1, 1000)] public int NumberOfAudioSources = 50;
        [Space]

        [Tooltip("The 'open' (un-occluded) cutoff frequency of the AudioLowPassFilter. This is the value a wall-checked " +
            "sound returns to when no wall is between it and the listener, and the baseline the per-layer reductions are " +
            "subtracted from. Keep it at ~22000 Hz (the top of human hearing) so an un-occluded sound is fully transparent; " +
            "lower values audibly dampen the high end (sounds muffled, as if behind a wall).")]
        [FormerlySerializedAs("defaultCuttoffFreqValue")]
        public float DefaultCutoffFreqValue = 22000f;
        [Space]

        [Tooltip("The minimum cutoff frequency value. The wall check will never reduce the frequency below this value.")]
        public float MinCutoffFreqValue = 100f;
        [Space]

        [Tooltip("How fast the low-pass cutoff glides toward its target when a sound moves in or out of occlusion, " +
            "in Hz per second. Smooths the transition so stepping out from behind a wall does not 'pop'. A higher " +
            "value tracks faster; 0 disables smoothing (the cutoff snaps instantly). Tune by ear — ~50000 transitions " +
            "a typical wall in roughly a third of a second.")]
        public float OcclusionSmoothingSpeed = 50000f;
        [Space]

        [Header(" --- Wallcheck Interval --- ")]
        [Tooltip("--Performance Tooltip-- : The time between two interval checks in seconds -> For human hearing: " +
                 " the sweet spot is between 0.1 to 0.25 -> Higher values means lower performance cost.")]
        [Range(0.01f, 1f)] public float TimeIntervalBetweenPositionChecks = 0.25f;
        [Space]

        [Header(" --- Per-layer wall damping --- ")]
        [Tooltip("Create a new element with (+) -> Expand it -> Choose a single Layer -> set its Wall Damping Factor " +
                 "(0 = transparent, 1 = fully muffled to MinCutoffFreqValue in a single wall). Walls combine multiplicatively.")]
        public WallDampingLayer[] WallDampingPerLayer;
        [Space]

        [Header(" --- Ducking --- ")]
        [Tooltip("--Performance Tooltip-- : Master switch for the whole ducking feature, read once at startup. " +
                 "Off means the duck service is never created: no rules are evaluated and the per-frame scan for " +
                 "active categories does not run at all. Leave it off unless you actually use duck rules below.")]
        public bool EnableDucking = false;
        [Space]

        [Tooltip("Each rule: while its trigger category is active (a sound of that category is playing and not " +
                 "paused), every listed target category is ducked to its factor. Strongest (lowest) factor wins " +
                 "when several triggers duck the same target. Only read when Enable Ducking is on.")]
        [SerializeField] private DuckRule[] duckRules;
        [Space]

        [Tooltip("How fast a category ducks DEEPER when a trigger starts, in factor units per second " +
                 "(e.g. 5 = full-to-half in 0.1s). 0 = instant, no smoothing. One global value for the whole system.")]
        [SerializeField] private float attackRate = 5f;

        [Tooltip("How fast a category RECOVERS toward full when its triggers stop, in factor units per second " +
                 "(e.g. 2 = half-to-full in 0.25s). 0 = instant, no smoothing. One global value for the whole system.")]
        [SerializeField] private float releaseRate = 2f;
        [Space]

        [Tooltip("Reserved for future mixer routing. Not read yet — declared so the two-gain-stage split has a " +
                 "home for stage-2 config without a later structural change.")]
        [SerializeField] private CategoryMixerRoute[] reservedMixerRoutes;
        [Space]

        [Header(" --- References ---")]
        [Tooltip("This transfer object contains all the audio source volumes which will be handled automatically by the " +
                 " [AudioTool]. Double click on this element to see which audio source volumes are handled at the moment")]
        public AudioVolumesTransferObject TransferObject;
        [Space]

        [Tooltip("This is simply the basic 3D Audio Object which will be pooled and used for this [AudioTool]")]
        public GameObject AudioGameObjectPrefab;

        public IReadOnlyList<DuckRule> Rules => duckRules;
        public float AttackRate => attackRate;
        public float ReleaseRate => releaseRate;

        /// <summary>
        /// Points out a master switch and a rule list that contradict each other. Both mismatches are completely
        /// silent at runtime — one burns a per-frame pool scan that can never duck anything, the other leaves
        /// authored rules unread — so the only place they can reasonably be caught is while the user is looking at
        /// the asset. The decision lives in <see cref="DuckConfigValidation"/>; only the wording belongs here.
        /// </summary>
        private void OnValidate()
        {
            switch (DuckConfigValidation.Evaluate(EnableDucking, duckRules?.Length ?? 0))
            {
                case DuckConfigIssue.EnabledWithoutRules:
                    Debug.LogWarning("[AudioTool] Enable Ducking is on, but no duck rules are configured. The " +
                                     "per-frame scan for active categories runs without being able to duck " +
                                     "anything. Add a rule below, or turn Enable Ducking off.", this);
                    break;

                case DuckConfigIssue.RulesWithoutEnabled:
                    Debug.LogWarning("[AudioTool] Duck rules are configured, but Enable Ducking is off, so they " +
                                     "are never read and nothing is ducked. Turn Enable Ducking on to use them.", this);
                    break;
            }
        }
    }
}
