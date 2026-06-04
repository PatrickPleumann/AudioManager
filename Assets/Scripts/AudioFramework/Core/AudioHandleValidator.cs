namespace AudioFramework.Core
{
    /// <summary>
    /// Pure, Unity-independent check for whether an <see cref="AudioHandle"/> still refers to the exact sound it was
    /// issued for. Takes raw ints (not an AudioHandle) so it stays trivially testable and has no dependency on the
    /// pool. Two guards: bounds (a handle index outside the pool is never current — closes the P6 {99999} crash) and
    /// generation equality (a slot's generation is bumped on every reuse, so an old handle to a recycled slot fails).
    /// </summary>
    public static class AudioHandleValidator
    {
        public static bool IsCurrent(int handleIndex, int handleGeneration, int slotGeneration, int poolLength)
        {
            if (handleIndex < 0 || handleIndex >= poolLength) return false;
            return handleGeneration == slotGeneration;
        }
    }
}
