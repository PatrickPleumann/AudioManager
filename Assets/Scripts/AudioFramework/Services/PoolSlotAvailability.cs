namespace AudioFramework.Pooling
{
    /// <summary>
    /// Pure, Unity-independent predicate for whether a pool slot is free for re-assignment. Extracted from
    /// <see cref="AudioPoolAcquisitionService.GetFreeAudioSourcePoolIndex"/> so the availability rule is
    /// EditMode-testable without a live AudioSource or Time.time. Takes raw values (not an AudioObject) to
    /// stay free of Unity. A slot is free only when it is silent, its OneShot busy-window has elapsed, and it
    /// is not one we paused — a paused source reports isPlaying == false, so without the pause guard a paused
    /// slot would be wrongly treated as free and overwritten (see the pause model in CLAUDE.md).
    /// </summary>
    public static class PoolSlotAvailability
    {
        public static bool IsFree(bool isPlaying, float currentTime, float busyUntilTime, bool isPaused)
            => !isPlaying && currentTime >= busyUntilTime && !isPaused;
    }
}
