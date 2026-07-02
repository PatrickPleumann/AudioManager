using System;
using UnityEngine;
using AudioFramework.Core;

namespace AudioFramework.Configuration
{
    /// <summary>
    /// One ducked target inside a <see cref="DuckRule"/>: while the rule's trigger category is active,
    /// this <see cref="Category"/> is ducked to <see cref="DuckedVolume"/> (a multiplicative factor in
    /// [0, 1]; 1 = untouched, 0.9 = to 90% of its volume, 0 = silenced — no inversion). Flattened into a
    /// <see cref="AudioFramework.Services.Mixing.DuckPair"/> for the resolver.
    /// </summary>
    [Serializable]
    public struct DuckTarget
    {
        [Tooltip("The category that gets ducked while the rule's trigger category is active.")]
        public AudioCategory Category;

        [Tooltip("The multiplicative factor this category is ducked TO while the trigger is active: " +
                 "1 = untouched, 0.9 = to 90% of its volume, 0.5 = to half, 0 = silenced. Lower = deeper duck. " +
                 "When several active triggers duck the same category, the strongest (lowest) wins.")]
        [Range(0f, 1f)] public float DuckedVolume;
    }
}
