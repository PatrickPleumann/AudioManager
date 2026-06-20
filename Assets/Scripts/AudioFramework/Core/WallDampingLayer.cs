using System;
using UnityEngine;

namespace AudioFramework.Core
{
    [Serializable]
    public struct WallDampingLayer
    {
        [SingleLayer] public int SingleLayer;

        [Tooltip("How strongly a wall on this layer damps the low-pass cutoff toward the floor: 0 = transparent " +
                 "(no effect), 1 = drops straight to MinCutoffFreqValue in a single wall. Across multiple walls the " +
                 "damping combines multiplicatively, so the cutoff approaches the floor but never crosses it. Typical 0.4–0.85.")]
        [Range(0f, 1f)] public float WallDampingFactor;
    }
}
