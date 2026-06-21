namespace AudioFramework.Services.WallCheck
{
    /// <summary>
    /// Pure, Unity-independent predicate for whether a wall-check loop should run another iteration for the exact
    /// dispatch it was started for. Extracted from the duplicated <c>ShouldContinueLoop</c> in both wall-check
    /// services so the continuation rule is EditMode-testable without a live AudioSource, and lives in ONE place.
    /// Takes raw values (not an AudioObject) to stay free of Unity.
    ///
    /// The first clause is the slot-currency guard (analogous to <see cref="AudioFramework.Core.AudioHandleValidator"/>):
    /// the loop captures the slot's generation when it starts, and stops the moment the slot's current generation no
    /// longer matches — i.e. the slot has been handed to a different dispatch. This closes the case where a non-
    /// wall-checked sound reuses the slot and an orphaned loop would otherwise keep raycasting for it. The generation
    /// check sits ABOVE the pause check on purpose: a reused slot may be paused for its new sound, but the old loop
    /// must still die. The destroyed-source case (a Unity Object lifetime concern + editor warning) stays in the
    /// service and never reaches here.
    /// </summary>
    public static class WallCheckContinuation
    {
        public static bool ShouldContinue(
            int startGeneration,
            int currentGeneration,
            bool isPaused,
            bool isOneShot,
            bool isPlaying,
            float currentTime,
            float busyUntilTime)
        {
            if (startGeneration != currentGeneration) return false;
            if (isPaused) return true;
            if (isOneShot) return isPlaying || currentTime < busyUntilTime;
            return isPlaying;
        }
    }
}
