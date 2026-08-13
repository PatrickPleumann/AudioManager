using System;
using UnityEngine;
using AudioFramework.Core;

namespace AudioFramework.Configuration
{
    /// <summary>
    /// One category's base volume as authored in the <see cref="AudioSystemConfig"/> — the value a settings
    /// slider starts from, and the first of the gain factors the volume chain combines. Lives in the config
    /// instead of in an asset of its own so that a whole volume set travels as ONE file the user can swap,
    /// and so nothing that looks like runtime state invites being written to at runtime.
    /// </summary>
    [Serializable]
    public struct CategoryVolume
    {
        [Tooltip("The category this volume applies to. List each category at most once — if one appears " +
                 "twice, the first entry wins and the duplicate is ignored.")]
        public AudioCategory Category;

        [Tooltip("The base volume every sound of this category plays at: 1 = unchanged, 0.5 = half, " +
                 "0 = silent. A category with no entry in this list plays at full volume.")]
        [Range(0f, 1f)] public float Volume;
    }
}
