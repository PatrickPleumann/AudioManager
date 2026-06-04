using AudioFramework.Services.Fading;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Test double for IFadeTarget: records the current volume and how often Stop() was called, so EditMode tests
    /// can assert what the fade service did without a real AudioSource.
    /// </summary>
    internal class FakeFadeTarget : IFadeTarget
    {
        public float Volume { get; set; }
        public bool IsPaused { get; set; }
        public int StopCallCount { get; private set; }
        public void Stop() => StopCallCount++;
    }
}
