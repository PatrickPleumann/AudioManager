using System;
using UnityEngine;
using AudioFramework.Core;

namespace AudioFramework.Configuration
{
    /// <summary>
    /// Serialized inspector-facing duck rule: while <see cref="Trigger"/> is active, each entry in
    /// <see cref="Targets"/> is ducked to its factor. Grouped by trigger for authoring convenience; flattened
    /// into <see cref="AudioFramework.Services.Mixing.DuckPair"/> rows (one per target) before the pure
    /// <see cref="AudioFramework.Services.Mixing.DuckTargetPolicy"/> consumes them.
    /// </summary>
    [Serializable]
    public struct DuckRule
    {
        [Tooltip("The category whose active playback triggers the duck. While any sound of this category is " +
                 "playing (and not paused), every target below is ducked.")]
        public AudioCategory Trigger;

        [Tooltip("The categories ducked while this trigger is active, each with its own duck factor.")]
        public DuckTarget[] Targets;
    }
}
