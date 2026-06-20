using UnityEngine;

using AudioFramework.Interfaces;

namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Resolves the scene's active AudioListener lazily and self-heals on access: the listener component is
    /// cached, but every call validates it (alive + active &amp; enabled) and re-resolves only when it went stale.
    /// A runtime listener swap (camera change, respawn, vehicle cam) is therefore picked up without polling — the
    /// expensive scene scan fires only in the swap moment, the steady state is a null-check plus a bool. The
    /// re-resolve deliberately picks an *active &amp; enabled* listener, because the common camera-switch pattern
    /// disables the old listener and enables a new one rather than destroying it. The decision (resolve or not)
    /// lives in the pure, EditMode-tested <see cref="ListenerCachePolicy"/>; this class is the Unity glue around it.
    /// </summary>
    public class SceneAudioListenerProvider : IAudioListenerProvider
    {
        private AudioListener cached;

        public SceneAudioListenerProvider(AudioListener _initialListener)
        {
            cached = _initialListener;
        }

        public bool TryGetPosition(out Vector3 _position)
        {
            bool hasCached = cached != null;
            bool isAliveAndActive = hasCached && cached.isActiveAndEnabled;

            if (ListenerCachePolicy.NeedsResolve(hasCached, isAliveAndActive))
                cached = ResolveActiveListener();

            if (cached == null)
            {
                _position = default;
                return false;
            }

            _position = cached.transform.position;
            return true;
        }

        private static AudioListener ResolveActiveListener()
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
                if (listeners[i].isActiveAndEnabled)
                    return listeners[i];
            return null;
        }
    }
}
