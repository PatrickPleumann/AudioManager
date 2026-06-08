using NUnit.Framework;
using AudioFramework.Services.WallCheck;

namespace AudioFramework.Tests.EditMode
{
    /// <summary>
    /// Tests for combining physics layer indices into a layer-mask bitmask. Every expected value is hand-derived
    /// from the agreed contract (each layer contributes 1 &lt;&lt; layer, OR-ed together), NOT read off the
    /// implementation. If the implementation disagrees with these, the implementation is wrong, not the test.
    /// </summary>
    public class WallLayerMaskTests
    {
        [Test]
        public void SingleLayer_SetsItsBit()
        {
            Assert.AreEqual(8, WallLayerMask.FromLayers(new[] { 3 })); // 1 << 3 = 8
        }

        [Test]
        public void MultipleLayers_OrTheirBits()
        {
            Assert.AreEqual(3, WallLayerMask.FromLayers(new[] { 0, 1 }));  // 1 | 2 = 3
            Assert.AreEqual(40, WallLayerMask.FromLayers(new[] { 3, 5 })); // 8 | 32 = 40
        }

        [Test]
        public void Empty_IsZero()
        {
            Assert.AreEqual(0, WallLayerMask.FromLayers(new int[0]));
        }

        [Test]
        public void Null_IsZero()
        {
            Assert.AreEqual(0, WallLayerMask.FromLayers(null));
        }
    }
}
