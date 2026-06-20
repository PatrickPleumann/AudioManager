using UnityEngine;

namespace AudioFramework.Interfaces
{
    /// <summary>
    /// Supplies the current world position of the active AudioListener for wall-check raycasts. Abstracted so the
    /// wall-check services no longer hold a raw (and potentially stale) Transform: a runtime listener swap
    /// (camera change, respawn, vehicle cam) is picked up via the provider instead of pointing forever at the
    /// listener resolved at startup. Returns false when there is no usable listener — callers then treat the
    /// sound as unoccluded (default cutoff), matching the previous "no listener" behaviour.
    /// </summary>
    public interface IAudioListenerProvider
    {
        bool TryGetPosition(out Vector3 _position);
    }
}
