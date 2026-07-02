using System;
using UnityEngine;
using UnityEngine.Audio;
using AudioFramework.Core;

namespace AudioFramework.Configuration
{
    /// <summary>
    /// RESERVED stage-2 seam (not yet applied): maps a category to an <see cref="AudioMixerGroup"/> for future
    /// routing/effects/reverb/sends/snapshots. Stage 1 (source.volume = base · fade · duck) is fully independent
    /// of this — Unity's signal path runs the mixer group strictly downstream of source.volume. Declared now only
    /// so the two-gain-stage split has a place for stage-2 config without a later structural change; nothing reads
    /// it yet.
    /// </summary>
    [Serializable]
    public struct CategoryMixerRoute
    {
        [Tooltip("The category to route.")]
        public AudioCategory Category;

        [Tooltip("Reserved for stage-2 mixer routing. Not applied yet.")]
        public AudioMixerGroup Group;
    }
}
