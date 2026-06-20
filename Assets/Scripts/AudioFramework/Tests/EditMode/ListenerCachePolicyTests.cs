using NUnit.Framework;
using AudioFramework.Services.WallCheck;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for the "must the cached AudioListener be re-resolved?" predicate. Every expected value is
    /// hand-derived from the agreed contract (resolve ⟺ no cached listener OR the cached one is no longer
    /// alive &amp; active), NOT read off the implementation. If the implementation disagrees with these, the
    /// implementation is wrong, not the test. Each test exercises one reachable clause of the predicate; the
    /// (hasCached: false, isAliveAndActive: true) row is unreachable (alive &amp; active implies a reference).
    /// </summary>
    public class ListenerCachePolicyTests
    {
        [Test]
        public void NoCachedListener_NeedsResolve()
        {
            // Nothing cached yet (startup, or a previous resolve found none) -> must resolve.
            Assert.IsTrue(ListenerCachePolicy.NeedsResolve(_hasCached: false, _isAliveAndActive: false));
        }

        [Test]
        public void CachedButStale_NeedsResolve()
        {
            // Cached listener went stale (destroyed on respawn, or disabled by a camera switch) -> must resolve.
            Assert.IsTrue(ListenerCachePolicy.NeedsResolve(_hasCached: true, _isAliveAndActive: false));
        }

        [Test]
        public void CachedAndAliveAndActive_DoesNotNeedResolve()
        {
            // Steady state: the cached listener is still the live, active one -> no resolve, just read it.
            Assert.IsFalse(ListenerCachePolicy.NeedsResolve(_hasCached: true, _isAliveAndActive: true));
        }
    }
}
