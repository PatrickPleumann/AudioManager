using NUnit.Framework;
using AudioFramework.Services.Fading;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests that a fade on a paused slot is FROZEN: Tick must not advance it, so it resumes correctly on unpause
    /// instead of running its ramp through silence. Separate file so the frozen suites stay untouched.
    /// </summary>
    public class AudioFadeServicePauseTests
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
        public void Tick_PausedSlot_DoesNotAdvance()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 0f, to: 1f, duration: 2f, stopOnEnd: false);
            targets[0].IsPaused = true;
            service.Tick(1f);
            Assert.AreEqual(0f, targets[0].Volume, Delta); // frozen at start, not advanced to 0.5
        }

        [Test]
        public void Tick_PausedThenResumed_ResumesFromWhereItWas()
        {
            var (service, targets) = MakeService(1);
            service.StartFade(0, from: 0f, to: 1f, duration: 2f, stopOnEnd: false);

            service.Tick(1f); // elapsed 1 -> 0.5
            Assert.AreEqual(0.5f, targets[0].Volume, Delta);

            targets[0].IsPaused = true;
            service.Tick(1f); // frozen
            Assert.AreEqual(0.5f, targets[0].Volume, Delta); // discriminating: a buggy Tick would complete to 1 here

            targets[0].IsPaused = false;
            service.Tick(1f); // elapsed 2 -> 1
            Assert.AreEqual(1f, targets[0].Volume, Delta);
        }

        [Test]
        public void Tick_PausedFadeOut_DoesNotCompleteOrStop()
        {
            var (service, targets) = MakeService(1);
            targets[0].Volume = 1f;
            service.StartFadeOut(0, duration: 2f);
            targets[0].IsPaused = true;
            service.Tick(5f);
            Assert.AreEqual(1f, targets[0].Volume, Delta);
            Assert.AreEqual(0, targets[0].StopCallCount);
        }
    }
}
