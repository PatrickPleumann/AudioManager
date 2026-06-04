using NUnit.Framework;
using AudioFramework.Services.Fading;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for StartFadeOut — the convenience that fades from the slot's CURRENT volume down to zero and stops.
    /// Separate file so the frozen AudioFadeServiceTests stay literally untouched. Expected values are hand-derived
    /// from the spec (same linear curve proven in AudioFadeMathTests).
    /// </summary>
    public class AudioFadeServiceFadeOutTests
    {
        private const float Delta = 1e-5f;

        private static (AudioFadeService service, FakeFadeTarget[] targets) MakeService(int slotCount)
        {
            var targets = new FakeFadeTarget[slotCount];
            var asInterfaces = new IFadeTarget[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                targets[i] = new FakeFadeTarget();
                asInterfaces[i] = targets[i];
            }
            return (new AudioFadeService(asInterfaces), targets);
        }

        [Test]
        public void StartFadeOut_StartsFromCurrentVolume()
        {
            var (service, targets) = MakeService(1);
            targets[0].Volume = 0.8f;
            service.StartFadeOut(index: 0, duration: 4f);
            Assert.AreEqual(0.8f, targets[0].Volume, Delta); // start value is wherever the sound currently is
        }

        [Test]
        public void StartFadeOut_RampsDownFromCurrentVolume()
        {
            var (service, targets) = MakeService(1);
            targets[0].Volume = 0.8f;
            service.StartFadeOut(0, duration: 4f);
            service.Tick(1f);
            Assert.AreEqual(0.6f, targets[0].Volume, Delta); // Evaluate(0.8, 0, 1, 4) = 0.6
        }

        [Test]
        public void StartFadeOut_Completes_ReachesZero_AndStops()
        {
            var (service, targets) = MakeService(1);
            targets[0].Volume = 0.8f;
            service.StartFadeOut(0, duration: 2f);
            service.Tick(2f);
            Assert.AreEqual(0f, targets[0].Volume, Delta);
            Assert.AreEqual(1, targets[0].StopCallCount);
        }
    }
}
