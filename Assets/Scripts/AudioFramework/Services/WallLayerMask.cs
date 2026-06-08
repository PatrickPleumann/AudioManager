using System.Collections.Generic;

namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Pure, Unity-independent combination of physics layer indices into a single layer-mask bitmask: each
    /// layer contributes <c>1 &lt;&lt; layer</c>, all OR-ed together. Extracted from the wall-check services so
    /// the mask generation is unit-testable without a play loop, and shared identically by both backends
    /// (UniTask and Coroutine) instead of being duplicated inline in each.
    /// </summary>
    public static class WallLayerMask
    {
        public static int FromLayers(IEnumerable<int> layers)
        {
            int mask = 0;
            if (layers != null)
                foreach (int layer in layers)
                    mask |= (1 << layer);
            return mask;
        }
    }
}
