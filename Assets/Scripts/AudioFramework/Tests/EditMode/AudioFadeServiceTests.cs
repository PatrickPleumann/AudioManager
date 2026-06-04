using NUnit.Framework;
using AudioFramework.Services.Fading;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests the fade orchestration via a fake target — no real AudioSource. Expected values are hand-derived from
    /// the spec (linear fade, same numbers proven in AudioFadeMathTests). The clobber-guard test is the important
    /// safety case: a cleared slot must never have its volume written again.
    /// </summary>
    public class AudioFadeServiceTests
    {
        private const float Delta = 1e-5f;

        private static (AudioFadeService service, FakeFadeTarget[] targets) MakeService(int slotCount)
        {
            var targets = new FakeFadeTarget[slotCount];
            for (int i = 0; i < slotCount; i++) targets[i] = new FakeFadeTarget();
            // Upcast to the interface array the service consumes.
            var asInterfaces = new IFadeTarget[slotCount];
            for (int i = 0; i < slotCount; i++) asInterfaces[i] = targets[i];
            return (new AudioFadeService(asInterfaces), targets);
        }

        [Test]
        public void StartFade_AppliesStartVolumeImmediately()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(index: 0, from: 0.2f, to: 1f, duration: 2f, stopOnEnd: false);
            Assert.AreEqual(0.2f, targets[0].Volume, Delta);
        }

        [Test]
        public void Tick_AdvancesActiveFade_WritesCurrentVolume()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 0f, to: 1f, duration: 2f, stopOnEnd: false);
            service.Tick(1f);
            Assert.AreEqual(0.5f, targets[0].Volume, Delta);
        }

        [Test]
        public void Tick_FadeInComplete_SettlesAtTarget_WithoutStopping()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 0f, to: 1f, duration: 2f, stopOnEnd: false);
            service.Tick(2f);
            Assert.AreEqual(1f, targets[0].Volume, Delta);
            Assert.AreEqual(0, targets[0].StopCallCount);
        }

        [Test]
        public void Tick_AfterSettle_DoesNotWriteVolumeAgain()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 0f, to: 1f, duration: 1f, stopOnEnd: false);
            service.Tick(1f); // completes & settles
            targets[0].Volume = 0.42f; // something else "takes over" the slot's volume
            service.Tick(1f);
            Assert.AreEqual(0.42f, targets[0].Volume, Delta);
        }

        [Test]
        public void Tick_FadeOutComplete_ReachesZero_AndStops()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 1f, to: 0f, duration: 2f, stopOnEnd: true);
            service.Tick(2f);
            Assert.AreEqual(0f, targets[0].Volume, Delta);
            Assert.AreEqual(1, targets[0].StopCallCount);
        }

        [Test]
        public void ClearFade_StopsFadeApplyingVolume_ClobberGuard()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 0f, to: 1f, duration: 2f, stopOnEnd: false);
            service.ClearFade(0);
            targets[0].Volume = 0.7f; // a new sound reuses the slot and sets its own volume
            service.Tick(1f);
            Assert.AreEqual(0.7f, targets[0].Volume, Delta); // must NOT be clobbered by the stale fade
        }

        [Test]
        public void Tick_DoesNotTouchInactiveSlots()
        {
            var (service, targets) = MakeService(2);
            service.StartFade(0, from: 0f, to: 1f, duration: 2f, stopOnEnd: false);
            targets[1].Volume = 0.9f; // slot 1 has no fade
            service.Tick(1f);
            Assert.AreEqual(0.5f, targets[0].Volume, Delta);
            Assert.AreEqual(0.9f, targets[1].Volume, Delta);
        }
    }
}
