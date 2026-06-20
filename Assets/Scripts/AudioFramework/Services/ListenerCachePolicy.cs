namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Pure, Unity-independent decision for whether the cached AudioListener must be re-resolved. Extracted from
    /// <see cref="SceneAudioListenerProvider"/> so the self-heal rule is EditMode-testable without live Unity
    /// objects. Re-resolve is needed when there is no cached listener at all (startup / none found) OR the cached
    /// one is no longer alive and active+enabled (destroyed on respawn, or disabled by a camera switch). The
    /// steady state — a cached listener that is still alive and active — needs no resolve.
    /// </summary>
    public static class ListenerCachePolicy
    {
        public static bool NeedsResolve(bool _hasCached, bool _isAliveAndActive)
            => !_hasCached || !_isAliveAndActive;
    }
}
